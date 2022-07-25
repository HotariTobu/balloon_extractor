namespace dataset_generator
{
    internal record Int32Point(int X, int Y)
    {
        public static readonly Int32Point Zero = new Int32Point(0, 0);
    }
}
