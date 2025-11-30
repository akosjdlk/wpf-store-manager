using StoreManager.Classes;
using System.Linq;
using System.Windows;

namespace StoreManager
{
	static class ListExtensions
	{
		public static void PrettyPrint<T>(this List<T> list)
		{
			Console.WriteLine("[\n\t" + string.Join(",\n\t", list) + "\n]");
		}
	}

	static class StringExtensions
	{
		public static string Title(this string input)
		{
			if (string.IsNullOrEmpty(input))
				return input;

			var words = input.Split(' ');
			for (int i = 0; i < words.Length; i++)
			{
				if (words[i].Length > 0)
					words[i] = char.ToUpper(words[i][0]) + words[i][1..].ToLower();
			}

			return string.Join(' ', words);
		}

	}
	public partial class App : Application
    {
        protected async void Application_Startup(object sender, StartupEventArgs e)
        {
            (await Product.GetAll()).PrettyPrint();
		}

    }
}
