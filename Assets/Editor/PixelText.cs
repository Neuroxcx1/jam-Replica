using System.Collections.Generic;
using UnityEngine;

// Letras de 5x7 pixeles para escribir en los sprites que se dibujan (carteles del laboratorio). Solo mayusculas.
public static class PixelText
{
    public const int Height = 7;

    // cada letra son 7 filas de 5 pixeles, de arriba a abajo: '#' = pintado
    static readonly Dictionary<char, string> Letters = new Dictionary<char, string>
    {
        ['A'] = ".###. #...# #...# ##### #...# #...# #...#",
        ['B'] = "####. #...# #...# ####. #...# #...# ####.",
        ['C'] = ".###. #...# #.... #.... #.... #...# .###.",
        ['D'] = "####. #...# #...# #...# #...# #...# ####.",
        ['E'] = "##### #.... #.... ####. #.... #.... #####",
        ['F'] = "##### #.... #.... ####. #.... #.... #....",
        ['G'] = ".###. #...# #.... #.### #...# #...# .###.",
        ['H'] = "#...# #...# #...# ##### #...# #...# #...#",
        ['I'] = ".###. ..#.. ..#.. ..#.. ..#.. ..#.. .###.",
        ['J'] = "..### ...#. ...#. ...#. ...#. #..#. .##..",
        ['K'] = "#...# #..#. #.#.. ##... #.#.. #..#. #...#",
        ['L'] = "#.... #.... #.... #.... #.... #.... #####",
        ['M'] = "#...# ##.## #.#.# #.#.# #...# #...# #...#",
        ['N'] = "#...# #...# ##..# #.#.# #..## #...# #...#",
        ['O'] = ".###. #...# #...# #...# #...# #...# .###.",
        ['P'] = "####. #...# #...# ####. #.... #.... #....",
        ['Q'] = ".###. #...# #...# #...# #.#.# #..#. .##.#",
        ['R'] = "####. #...# #...# ####. #.#.. #..#. #...#",
        ['S'] = ".#### #.... #.... .###. ....# ....# ####.",
        ['T'] = "##### ..#.. ..#.. ..#.. ..#.. ..#.. ..#..",
        ['U'] = "#...# #...# #...# #...# #...# #...# .###.",
        ['V'] = "#...# #...# #...# #...# #...# .#.#. ..#..",
        ['W'] = "#...# #...# #...# #.#.# #.#.# #.#.# .#.#.",
        ['X'] = "#...# #...# .#.#. ..#.. .#.#. #...# #...#",
        ['Y'] = "#...# #...# .#.#. ..#.. ..#.. ..#.. ..#..",
        ['Z'] = "##### ....# ...#. ..#.. .#... #.... #####",
    };

    // 5 pixeles por letra y 1 de separacion
    public static int Width(string text) => text.Length * 6 - 1;

    // escribe el texto con su esquina de abajo a la izquierda en (x, y); lo que no sea una letra deja un hueco
    public static void Draw(Texture2D texture, string text, int x, int y, Color color)
    {
        foreach (char c in text)
        {
            if (Letters.TryGetValue(c, out string letter))
            {
                string[] rows = letter.Split(' ');
                for (int row = 0; row < Height; row++)
                    for (int column = 0; column < 5; column++)
                        if (rows[row][column] == '#') texture.SetPixel(x + column, y + Height - 1 - row, color);
            }
            x += 6;
        }
    }
}
