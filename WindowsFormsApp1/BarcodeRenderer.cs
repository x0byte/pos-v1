using System;
using System.Collections.Generic;
using System.Drawing;

namespace WindowsFormsApp1
{
    public static class BarcodeRenderer
    {
        public static Bitmap GenerateCode39Barcode(string code, int availableWidth, int barHeight)
        {
            var patterns = new Dictionary<char, string>
            {
                {'0',"000110100"}, {'1',"100100001"}, {'2',"001100001"}, {'3',"101100000"},
                {'4',"000110001"}, {'5',"100110000"}, {'6',"001110000"}, {'7',"000100101"},
                {'8',"100100100"}, {'9',"001100100"}, {'A',"100001001"}, {'B',"001001001"},
                {'C',"101001000"}, {'D',"000011001"}, {'E',"100011000"}, {'F',"001011000"},
                {'G',"000001101"}, {'H',"100001100"}, {'I',"001001100"}, {'J',"000011100"},
                {'K',"100000011"}, {'L',"001000011"}, {'M',"101000010"}, {'N',"000010011"},
                {'O',"100010010"}, {'P',"001010010"}, {'Q',"000000111"}, {'R',"100000110"},
                {'S',"001000110"}, {'T',"000010110"}, {'U',"110000001"}, {'V',"011000001"},
                {'W',"111000000"}, {'X',"010010001"}, {'Y',"110010000"}, {'Z',"011010000"},
                {'-',"010000101"}, {'.',"110000100"}, {' ',"011000100"}, {'*',"010010100"},
                {'$',"010101000"}, {'/',"010100010"}, {'+',"010001010"}, {'%',"000101010"}
            };

            string raw = "*" + (code ?? string.Empty).ToUpperInvariant() + "*";
            var chars = new List<char>();
            foreach (char c in raw)
            {
                if (patterns.ContainsKey(c))
                {
                    chars.Add(c);
                }
            }

            int n = Math.Max(chars.Count, 2);
            int narrowWidth = Math.Max(1, availableWidth / (16 * n - 1));
            int wideWidth = narrowWidth * 3;

            int totalWidth = 0;
            foreach (char c in chars)
            {
                string pat = patterns[c];
                for (int i = 0; i < 9; i++)
                {
                    totalWidth += pat[i] == '1' ? wideWidth : narrowWidth;
                }
            }
            totalWidth += (chars.Count - 1) * narrowWidth;

            Bitmap bmp = new Bitmap(Math.Max(totalWidth, 1), barHeight);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                int x = 0;
                for (int ci = 0; ci < chars.Count; ci++)
                {
                    string pat = patterns[chars[ci]];
                    for (int i = 0; i < 9; i++)
                    {
                        int w = pat[i] == '1' ? wideWidth : narrowWidth;
                        if (i % 2 == 0)
                        {
                            g.FillRectangle(Brushes.Black, x, 0, w, barHeight);
                        }
                        x += w;
                    }
                    if (ci < chars.Count - 1)
                    {
                        x += narrowWidth;
                    }
                }
            }

            return bmp;
        }
    }
}
