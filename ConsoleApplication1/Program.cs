using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class Example
{
    const string lexeme = @"\s*\b[_a-zA-Z][_a-zA-Z0-9]*\b";

    public static string lastWord(string matchString)
    {
        var splitedString = matchString.Split(' ');
        return splitedString[splitedString.Length - 1];
    }

    public static string addType(string types, string newType)
    {
        types = types.Substring(0, types.Length - 4);
        return types + @"|" + newType + @")\s+";
    }

    public static void addDeclaration(string matchedLexeme, List<string> names, List<int> counters)
    {
        if (names.IndexOf(matchedLexeme) == -1)
        {
            names.Add(matchedLexeme);
            counters.Add(-1);
        }
        else
            --counters[names.IndexOf(matchedLexeme)];
    }

    public static void countInitialization(string codeLine, string matchedLexeme, List<string> names, List<int> counters)
    {
        string initPattern = matchedLexeme + @"\s*=";
        foreach (Match match in Regex.Matches(codeLine, initPattern, RegexOptions.IgnoreCase))
            ++counters[names.IndexOf(matchedLexeme)];
    }

    public static void findCommaDeclarations(string codeLine, string firstVariable, string types, List<string> names, List<int> counters)
    {
        string commaDeclarationPattern = firstVariable + @"[\S\s^,]*?,(?!" + types + @")" + lexeme;
        Match commaMatch = Regex.Match(codeLine, commaDeclarationPattern, RegexOptions.None);
        while (commaMatch.Success)
        {
            addDeclaration(lastWord(commaMatch.Value), names, counters);
            countInitialization(codeLine, lastWord(commaMatch.Value), names, counters);
            commaDeclarationPattern = @"\b" + lastWord(commaMatch.Value) + @"\b[\S\s^,]*?,(?!" + types + @")" + lexeme;
            commaMatch = Regex.Match(codeLine, commaDeclarationPattern, RegexOptions.None);
        }
    }

    public static void countSpan(string codeLine, List<string> names, List<int> counters)
    {
        foreach (string name in names)
        {
            string searchPattern = @"\b" + name + @"\b";
            foreach (Match match in Regex.Matches(codeLine, searchPattern, RegexOptions.None))
                ++counters[names.IndexOf(name)];
        }
    }

    public static void output(List<string> names, List<int> counters)
    {
        foreach (string name in names)
        {
            Console.WriteLine("Спен переменной " + name + " равен " + counters[names.IndexOf(name)] + ".");
            Console.WriteLine();
        }
    }

    public static void Main()
    {
        string codeLine;
        string patternTypedef = @"typedef\s*\b[_a-zA-Z0-9]+\b\s*\b[_a-zA-Z0-9]+\b";
        List<string> names = new List<string>();
        List<int> counters = new List<int>();
        string types = @"\s*(int|signed|unsigned|short|long|char|float|double)\s+";
        string typeDeclaration = types + @"(?!" + types + @")" + lexeme;
        System.IO.StreamReader file = new System.IO.StreamReader(@"c.txt");
        while ((codeLine = file.ReadLine()) != null)
        {
            Match typeMatch = Regex.Match(codeLine, patternTypedef, RegexOptions.None);
            if (typeMatch.Success)
            {
                types = addType(types, lastWord(typeMatch.Value));
                typeDeclaration = types + @"(?!" + types + @")" + lexeme;
            }
            else
            foreach (Match match in Regex.Matches(codeLine, typeDeclaration, RegexOptions.None))
            {
                addDeclaration(lastWord(match.Value), names, counters);
                countInitialization(codeLine, lastWord(match.Value), names, counters);
                findCommaDeclarations(codeLine, lastWord(match.Value), types, names, counters);
            }
            countSpan(codeLine, names, counters);
        }
        output(names, counters);
        file.Close();
        Console.ReadLine();
    }
}