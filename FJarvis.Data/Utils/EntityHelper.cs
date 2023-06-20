using System.Text;

namespace FJarvis.Data.Utils
{
    public static class EntityHelper
    {
        public static bool[] DecompressBitmask(string compressed)
        {
            string[] chunksStr = compressed.Split(';', StringSplitOptions.RemoveEmptyEntries);
            int numChunks = chunksStr.Length;
            ulong[] chunks = new ulong[numChunks];
            bool[] bitFlags = new bool[numChunks * 64];
    
            for (int i = 0; i < numChunks; i++)
            {
                chunks[i] = ulong.Parse(chunksStr[i]);
                string binaryStr = Convert.ToString((long)chunks[i], 2).PadLeft(64, '0');
                for (int j = 0; j < 64; j++)
                {
                    bitFlags[i * 64 + j] = binaryStr[j] == '1';
                }
            }

            return bitFlags;
        }

        public static string CompressBitmask(string binaryStr)
        {
            // Remove trailing semicolon if present
            if (binaryStr.EndsWith(";"))
            {
                binaryStr = binaryStr.Remove(binaryStr.Length - 1);
            }

            string[] chunks = binaryStr.Split(';');

            bool[] bitFlags = new bool[chunks.Length * 64];

            for (int i = 0; i < chunks.Length; i++)
            {
                if (chunks[i].Length != 64)
                    throw new ArgumentException("Invalid bitmask string. 64-bit chunks must be 64 characters long.");
                    
                string chunkStr = chunks[i].PadLeft(64, '0');
                for (int j = 0; j < 64; j++)
                {
                    bitFlags[i * 64 + j] = chunkStr[j] == '1';
                }
            }
            return CompressBitmask(bitFlags);
        }
        
        public static string CompressBitmask(bool[] bitFlags)
        {
            
        
            // Calculate the number of 64-bit chunks required to represent the entire boolean array
            int numChunks = (int)Math.Ceiling(bitFlags.Length / 64.0);

            // Create an array to hold the 64-bit chunks
            ulong[] chunks = new ulong[numChunks];
    
            // Loop through each chunk and set the bits according to the boolean array
            for (int i = 0; i < numChunks; i++)
            {
                // Each chunk is 64 bits in length, so loop through each bit in the chunk
                for (int j = 0; j < 64; j++)
                {
                    // Shift the current value of the chunk one bit to the left and OR it with the next bit from the boolean array
                    // This effectively sets the current bit in the chunk to the value of the corresponding boolean value in the array
                    chunks[i] = (chunks[i] << 1) | (ulong)(bitFlags[i * 64 + j] ? 1 : 0);
                }
            }

            // Convert the 64-bit chunks to a compressed string representation
            StringBuilder sb = new StringBuilder();
            foreach (ulong chunk in chunks)
            {
                sb.Append(chunk).Append(';');
            }

            return sb.ToString();
        }
          
        public static String DecompressBitMaskAsString(string compressed)
        {
            var decompMask= DecompressBitmask(compressed);
            var result = string.Join("", decompMask.Select(b => (b ? "1" : "0").PadLeft(1, '0')));
            return result;
        }
    }
  

}