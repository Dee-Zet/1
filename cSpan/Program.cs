using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class Example
{
    const string lexeme = @"\s*\b[_a-zA-Z][_a-zA-Z0-9]*\b";

    public static string fileChoseDialog()
    {
        Console.WriteLine ("Пожалуйста, введите путь к файлу:");
        string fileName = Console.ReadLine();
        if (System.IO.File.Exists(fileName))
            return fileName;
        else
            return null;
    }

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
        Console.WriteLine();
        foreach (string name in names)
            Console.WriteLine("Спен переменной " + name + "\t= " + counters[names.IndexOf(name)] + ".");
    }

    public static void prepareFile(string fileName)
    {
        System.IO.StreamReader file = new System.IO.StreamReader(fileName);
        string codeFile = file.ReadToEnd();
        file.Close();
        string stringSearchPattern = "\"[^\"]*\"";
        string replacement = "\"\"";
        Regex rgx = new Regex(stringSearchPattern);
        codeFile = rgx.Replace(codeFile, replacement);
        stringSearchPattern = @"/\*[\S\s]*\*/";
        replacement = @" ";
        rgx = new Regex(stringSearchPattern);
        codeFile = rgx.Replace(codeFile, replacement);
        stringSearchPattern = "//[^\n]*";
        replacement = "\r";
        rgx = new Regex(stringSearchPattern);
        codeFile = rgx.Replace(codeFile, replacement);

        /*stringSearchPattern = "\r\n[^;]*\r\n";
        foreach (Match match in Regex.Matches(codeFile, stringSearchPattern, RegexOptions.None))
        {
            replacement = match.Value.Substring(0, match.Value.Length - 2);
            rgx = new Regex(match.Value);
            codeFile = rgx.Replace(codeFile, replacement);
        }*/

        System.IO.File.WriteAllText(fileName + @"~", codeFile);
    }

    public static void Main()
    {
        int bracketCounter = 0;
        string codeLine;
        string patternTypedef = @"typedef\s*\b[_a-zA-Z0-9]+\b\s*\b[_a-zA-Z0-9]+\b";
        List<string> globalNames = new List<string>();
        List<int> globalCounters = new List<int>();
        List<string> localNames = new List<string>();
        List<int> localCounters = new List<int>();
        string types = @"\s*(int|signed|unsigned|short|long|char|float|double)\s+";
        string typeDeclaration = types + @"(?!" + types + @")" + lexeme + @"(?!(\s*\())";
        string fileName = fileChoseDialog();
        if (fileName != null)
        {
            prepareFile(fileName);
            System.IO.StreamReader file = new System.IO.StreamReader(fileName + @"~");
            while ((codeLine = file.ReadLine()) != null)
            {
                if (codeLine.Contains("{"))
                    bracketCounter++;
                Match typeMatch = Regex.Match(codeLine, patternTypedef, RegexOptions.None);
                if (typeMatch.Success)
                {
                    types = addType(types, lastWord(typeMatch.Value));
                    typeDeclaration = types + @"(?!" + types + @")" + lexeme + @"(?!(\s*\())";
                }
                else
                    foreach (Match match in Regex.Matches(codeLine, typeDeclaration, RegexOptions.None))
                        if (bracketCounter == 0)
                        {
                            addDeclaration(lastWord(match.Value), globalNames, globalCounters);
                            countInitialization(codeLine, lastWord(match.Value), globalNames, globalCounters);
                            findCommaDeclarations(codeLine, lastWord(match.Value), types, globalNames, globalCounters);
                        }
                        else
                        {
                            addDeclaration(lastWord(match.Value), localNames, localCounters);
                            countInitialization(codeLine, lastWord(match.Value), localNames, localCounters);
                            findCommaDeclarations(codeLine, lastWord(match.Value), types, localNames, localCounters);
                        }
                if (bracketCounter == 0)
                    countSpan(codeLine, globalNames, globalCounters);
                else
                    countSpan(codeLine, localNames, localCounters);
                if (codeLine.Contains("}"))
                    bracketCounter--;
                //countSpan(codeLine, localNames, localCounters);
            }
            output(globalNames, globalCounters);
            output(localNames, localCounters);
            file.Close();
        }
        else
            Console.WriteLine("Файл по данному пути отсутствует.");
        Console.ReadLine();
    }
}