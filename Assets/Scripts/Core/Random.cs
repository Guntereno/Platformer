using System;

namespace Core
{
	public static class Random
	{
        public static void Shuffle<T>(System.Random random, T[] array, T[] selectionPool)
        {
            // Implementation of Fisher-Yates shuffle in place
            // https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle

            if (array.Length <= 1)
            {
                return;
            }

            if(array.Length != selectionPool.Length)
			{
                throw new ArgumentException("Arrays must be the same size!");
			}

            int length = array.Length;
            Array.Copy(array, selectionPool, length);

            int selectionCount = length;
            do
            {
                // Choose a random point from the selection pool
                int selectionIndex = random.Next(0, (selectionCount - 1));
                T selection = selectionPool[selectionIndex];

                // Assign it to the destination pool
                int destinationIndex = length - selectionCount;
                array[destinationIndex] = selection;

                // Move end into selection position
                selectionPool[selectionIndex] = selectionPool[selectionCount - 1];

                --selectionCount;
            } while (selectionCount > 0);
        }
    }
}
