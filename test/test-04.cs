
using System;

class Test {

	static int Fib (int n)
	{
		if (n < 2)
			return 1;
		return Fib (n-1) + Fib (n-2);
	}

	static void Main ()
	{
		Console.WriteLine (Fib (20));
	}

}
