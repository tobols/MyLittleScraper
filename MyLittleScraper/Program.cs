// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");


string choice = null;

do
{
    Console.Write("Url: ");
    choice = Console.ReadLine();
} while (choice == null);


Console.WriteLine(choice);





/* TODO List:
 * 
 * Fix httpclient etc..
 * Recursive search through site
 * Only read pages once, save visited urls
 * Do it in parallell
 * Save everything
 * 
 */