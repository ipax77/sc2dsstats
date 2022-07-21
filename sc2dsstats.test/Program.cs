using System.Reflection;
// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

var loc = Assembly.GetExecutingAssembly().Location;

Console.WriteLine($"Location: {loc}");