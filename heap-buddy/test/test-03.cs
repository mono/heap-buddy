
class Test {

	static void Recursive (int i)
	{
		if (i > 0) {
			int [] foo;
			foo = new int [20];
			foo [0] = i;
			Recursive (i-1);
		}
	}

	static void Main ()
	{
		Recursive (2000);
	}

}
