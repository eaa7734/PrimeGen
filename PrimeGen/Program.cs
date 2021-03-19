using System;
using System.Diagnostics;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading.Tasks;

/*
 * Author: Edgar Argueta
 * Purpose: This project generates prime numbers based on commandline arguments
 * by randomly generating a sequence of bits and checking if it is prime.
 */
namespace PrimeGen
{
    /*
     * This is driver class called program that handles the initialization of things.
     */
    class Program
    {
        /*
         * In main we handle the commandline arguments that were passed in and process them to make sure they
         * are usable. We then initialize the objects we need and begin generating prime numbers. 
         */
        static void Main(string[] args)
        {
            try
            {
                // Need at least the number of bits to be passed in
                if (args.Length != 1 && args.Length != 2) throw new Exception();
                var bits = Convert.ToInt32(args[0]);
                var numPrimes = 1; // Set default value if number of prime numbers was not passed in
                if (bits % 8 != 0 || bits < 32)  // Make sure the number of bits can be converted to bytes
                {
                    throw new Exception();
                }
                if (args.Length == 2)
                {
                    numPrimes = Convert.ToInt32(args[1]);
                }
                Console.WriteLine("BitLength: " + bits + " bits");
                var generator = new PrimeNumberGenerator(bits / 8, numPrimes); 
                var watch = new Stopwatch();
                watch.Start(); // Start timer
                generator.Run(); // Begin generating prime numbers
                watch.Stop(); // Stop timer
                Console.WriteLine("Time to Generate: {0}", watch.Elapsed); // Print time elapsed
            }
            catch (Exception)
            {
                // Print out help message if an error occurred or incorrect command line arguments were passed in.
                Console.WriteLine("Usage: <bits> <count=1>\n" +
                                  "\t - bits - the number of bits of the prime number, this must be a" +
                                  "multiple of 8, and at least 32 bits.\n" +
                                  "\t - count - the number of prime numbers to generate, defaults to 1.");
            }
        }
        
    }
    
    /*
     * PrimeNumberGenerator is a class that will generate the specified amount of prime numbers at
     * the desired length of using Parallelization to improve performance. The basic algorithm used is to create a byte
     * array at the desired length and randomly fill it with values using RNGCryptoServiceProvider class. Then, turn it
     * into a BigInteger and check to see if it is a prime number. It is a prime number we let our helper class Counter
     * know and it will handle the rest. 
     */
    class PrimeNumberGenerator
    {
        public Counter _myCounter;
        private static RNGCryptoServiceProvider _rngCrypto = new RNGCryptoServiceProvider();
        private int _byteSize;

        public PrimeNumberGenerator(int byteSize, int maxPrimes)
        {
            _byteSize = byteSize;
            _myCounter = new Counter(maxPrimes);
        }

        /*
         * Run method for PrimeNumberGenerator that initializes all the threads and has them start generating random
         * numbers. If a prime number is generated it then calls FoundPrime from the Counter helper class. Once the
         * specified amount of prime numbers has been printed the threads are stopped.
         */
        public void Run()
        {
            Parallel.For(0, int.MaxValue, (generators, state) =>
            {
                while (!_myCounter.DoneGenerating) // Continue generating a number until we are done
                {
                    var guess = GuessPrime(); // Guess a number
                    if (IsProbablyPrime(guess)) // Check to see if it is prime
                    {
                        _myCounter.FoundPrime(guess); // Let counter know we found a prime number
                    }
                }
                state.Stop();

            });
        }

        /*
         * Function guesses a prime number by generating a new byte array and using RNGCryptoServiceProvider
         * to randomly fill the byte array with bits. Then we convert the byte array to a BigInteger and return it.
         */
        private BigInteger GuessPrime()
        {
            var temp = new byte[_byteSize];
            _rngCrypto.GetBytes(temp);
            return new BigInteger(temp);
        }
        
        /*
         * This function checks to see if a BigInteger value is prime. Given by the Prof.
         */
        static Boolean IsProbablyPrime(BigInteger value, int witnesses = 10)
        {
            if (value <= 1) return false;
            
            if (witnesses <= 0) witnesses = 10;
            
            BigInteger d = value - 1;
            int s = 0;
            
            while (d % 2 == 0)
            {
                d /= 2;
                s += 1;
            }

            Byte[] bytes = new Byte[value.ToByteArray().LongLength];
            BigInteger a;

            for (int i = 0; i < witnesses; i++)
            {
                do
                {
                    var Gen = new Random();
                    Gen.NextBytes(bytes);
                    a = new BigInteger(bytes);
                } while (a < 2 || a >= value - 2);
                
                BigInteger x = BigInteger.ModPow(a, d, value);
                if (x == 1 || x == value - 1) continue;

                for (int r = 1; r < s; r++)
                {
                    x = BigInteger.ModPow(x, 2, value);
                    if (x == 1) return false;
                    if (x == value - 1) break;
                }

                if (x != value - 1) return false;
            }

            return true;
        }
    }

    /*
     * Helper class to keep track of counting the number of primes we have calculated and how many more
     * we need to calculate. It also handles printing out the prime number.
     */
    class Counter
    {
        private static object _myLock = new();
        private int _curPrimes;
        private int _maxPrimes;
        public bool DoneGenerating { get; private set; }
        public Counter(int maxPrimes)
        {
            _curPrimes = 0;
            _maxPrimes = maxPrimes;
            DoneGenerating = false;
        }

        /*
         * This function will allow only one thread to access it and it is called when a thread has found
         * a prime number. It then weeds out any threads if we are supposed to be done generating prime numbers.
         * If we still need to print out prime numbers it increments the counter and prints the prime number.
         */
        public void FoundPrime(BigInteger prime)
        {
            lock (_myLock) // Make sure only 1 thread can access this at a time
            {
                if (DoneGenerating) return; // In case if a thread was not stopped but we are done generating
                _curPrimes += 1;
                if (_curPrimes == 1)
                {
                    Console.WriteLine(_curPrimes + ": " + prime);
                }
                else
                {
                    Console.WriteLine("\n" + _curPrimes + ": " + prime);
                }
                if (_curPrimes >= _maxPrimes) DoneGenerating = true; // We are done generating primes
            }
        }

    }
}