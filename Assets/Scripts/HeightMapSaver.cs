using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class HeightMapSaver
{
	public static void SaveToCsv(float[,] heightMap, string name)
	{
		FileStream stream;
		if (File.Exists(name + ".csv"))
			File.Delete(name + ".csv");
		stream = File.Open(name + ".csv", FileMode.Create);

		StreamWriter streamWriter = new StreamWriter(stream);

		for (int z = 0; z < heightMap.GetLength(0); ++z)
		{
			for (int x = 0; x < heightMap.GetLength(1); ++x)
			{
				streamWriter.Write(heightMap[z, x]);
				streamWriter.Write(',');
			}
			streamWriter.WriteLine();
		}

		streamWriter.Close();
		stream.Close();
	}

	public static void SaveToCsv(string[,] table, string name)
	{
		FileStream stream;
		if (File.Exists(name + ".csv"))
			File.Delete(name + ".csv");
		stream = File.Open(name + ".csv", FileMode.Create);

		StreamWriter streamWriter = new StreamWriter(stream);

		for (int z = 0; z < table.GetLength(0); ++z)
		{
			for (int x = 0; x < table.GetLength(1); ++x)
			{
				streamWriter.Write(table[z, x]);
				streamWriter.Write(',');
			}
			streamWriter.WriteLine();
		}

		streamWriter.Close();
		stream.Close();
	}

	public static void SaveToTexture(float[,] heightMap, string name)
	{
		Texture2D texture = new Texture2D(heightMap.GetLength(1), heightMap.GetLength(0));

		for (int z = 0; z < heightMap.GetLength(0); ++z)
		{
			for (int x = 0; x < heightMap.GetLength(1); ++x)
			{
				texture.SetPixel(x, z, new Color(heightMap[z, x], heightMap[z, x], heightMap[z, x]));
			}
		}

		FileStream stream;
		if (File.Exists(name + ".png"))
			File.Delete(name + ".png");
		stream = File.Open(name + ".png", FileMode.Create);
		BinaryWriter streamWriter = new BinaryWriter(stream);

		byte[] data = texture.EncodeToPNG();

		streamWriter.Write(data);

		streamWriter.Close();
		stream.Close();
	}

	public static void SaveTexTable(float[,] table, string fileName, string caption)
	{
		FileStream stream;
		if (File.Exists(fileName + ".tex"))
			File.Delete(fileName + ".tex");
		stream = File.Open(fileName + ".tex", FileMode.Create);

		StreamWriter streamWriter = new StreamWriter(stream);

		streamWriter.WriteLine("\\begin{table}[ht]");
		streamWriter.WriteLine("\\centering");
		streamWriter.WriteLine("\\caption{" + caption + "}");

		streamWriter.Write("\\begin{tabularx}{1\\textwidth}{ |");
		streamWriter.Write(">{\\centering\\arraybackslash}X||");
		for (int z = 0; z < table.GetLength(0); ++z)
		{
			streamWriter.Write(">{\\centering\\arraybackslash}X|");
		}
		streamWriter.WriteLine("}");
		streamWriter.WriteLine("\\hline");
		streamWriter.Write("z/x & ");
		for (int x = 0; x < table.GetLength(0); ++x)
		{
			streamWriter.Write(x);
			if (x != table.GetLength(0) - 1)
				streamWriter.Write(" & ");
		}
		streamWriter.WriteLine("\\\\");
		streamWriter.WriteLine("\\hline");

		for (int z = 0; z < table.GetLength(0); ++z)
		{
			streamWriter.WriteLine("\\hline");

			for (int x = 0; x < table.GetLength(1); ++x)
			{
				if (x == 0)
				{
					streamWriter.Write(z);
					streamWriter.Write(" & ");
				}

				streamWriter.Write(Math.Round(table[z, x], 2));
				if (x != table.GetLength(1) - 1)
					streamWriter.Write(" & ");
			}
			streamWriter.WriteLine("\\\\");
		}
		streamWriter.WriteLine("\\hline");

		streamWriter.WriteLine("\\end{tabularx}");
		streamWriter.WriteLine("\\label{table:" + fileName + "}");
		streamWriter.WriteLine("\\end{table}");

		streamWriter.Close();
		stream.Close();
	}

	public static void SaveTexTable(string[,] table, string fileName, string caption)
	{
		FileStream stream;
		if (File.Exists(fileName + ".tex"))
			File.Delete(fileName + ".tex");
		stream = File.Open(fileName + ".tex", FileMode.Create);

		StreamWriter streamWriter = new StreamWriter(stream);

		streamWriter.WriteLine("\\begin{table}[h]");
		streamWriter.WriteLine("\\centering");
		streamWriter.WriteLine("\\caption{" + caption + "}");

		streamWriter.Write("\\begin{tabularx}{1\\textwidth}{ |");
		streamWriter.Write(">{\\centering\\arraybackslash}X||");
		for (int x = 0; x < table.GetLength(0); ++x)
		{
			streamWriter.Write(">{\\centering\\arraybackslash}X|");
		}
		streamWriter.WriteLine("}");
		streamWriter.WriteLine("\\hline");

		//write first row
		for (int x = 0; x < table.GetLength(1); ++x)
		{
			streamWriter.Write(table[0, x]);
			if (x != table.GetLength(1) - 1)
				streamWriter.Write(" & ");
		}
		streamWriter.WriteLine("\\\\");
		streamWriter.WriteLine("\\hline");

		//write rest of the rows

		for (int z = 1; z < table.GetLength(0); ++z)
		{
			streamWriter.WriteLine("\\hline");

			for (int x = 0; x < table.GetLength(1); ++x)
			{
				streamWriter.Write(table[z, x]);
				if (x != table.GetLength(1) - 1)
					streamWriter.Write(" & ");
			}
			streamWriter.WriteLine("\\\\");
		}
		streamWriter.WriteLine("\\hline");

		streamWriter.WriteLine("\\end{tabularx}");
		streamWriter.WriteLine("\\label{table:" + fileName + "}");
		streamWriter.WriteLine("\\end{table}");

		streamWriter.Close();
		stream.Close();
	}
}