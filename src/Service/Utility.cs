using System.Text;

internal static class Utility
{
    
    public static int RomanToInt(string s)
    {
        var dic = new Dictionary<char, int>();
        dic.Add('I', 1);
        dic.Add('V', 5);
        dic.Add('X', 10);
        dic.Add('L', 50);
        dic.Add('C', 100);
        dic.Add('D', 500);
        dic.Add('M', 1000);

        int ret = 0;
        int len = s.Length;
        for (int i = 0; i < len; i++)
        {
            var val = dic[s[i]];
            if (i + 1 < len && dic[s[i + 1]] > val)
            {
                ret -= val;
            }
            else
            {
                ret += val;
            }
        }
        
        return ret;
    }

    // IntToRoman(1994)); // Output: MCMXCIV
    public static string IntToRoman(int input)
    {
        var dic = new Dictionary<char, int>
        {
            { 'I', 1 },
            { 'V', 5 },
            { 'X', 10 },
            { 'L', 50 },
            { 'C', 100 },
            { 'D', 500 },
            { 'M', 1000 }
        };
        var map = dic.Select((x => x.Value)).ToArray();
        
        var stack = new Stack<char>();
        var inputs = input.ToString().ToCharArray();
        var pos = 0;
        for (var i = inputs.Length - 1; i >-1; i--)
        {
            var val = int.Parse(inputs[i].ToString());
            var pow = (int)Math.Pow(10, inputs.Length - 1 - i);
            val *= pow;
            if (val == map[pos] || val == map[pos + 1])
            {
                stack.Push(dic.First(x => x.Value == map[pos]).Key);
            }
            else if (val == (map[pos + 1] - pow))
            {
                stack.Push(dic.First(x => x.Value == map[pos + 1]).Key);
                stack.Push(dic.First(x => x.Value == map[pos]).Key);
            }
            else if (val == (map[pos + 2] - pow))
            {
                stack.Push(dic.First(x => x.Value == map[pos + 2]).Key);
                stack.Push(dic.First(x => x.Value == map[pos]).Key);
            }

            pos += 2;
        }        
        return string.Join("", stack);
    }

    public static string IntToRoman2(int num)
    {
        // Pre‑sorted from largest → smallest
        int[] values =    [1000, 900, 500, 400, 100,  90,  50,  40,  10,   9,   5,   4,   1];
        string[] symbols =["M", "CM","D", "CD","C", "XC","L", "XL","X", "IX","V", "IV","I"];

        var sb = new StringBuilder();

        for (int i = 0; i < values.Length; i++)
        {
            while (num >= values[i])
            {
                num -= values[i];
                sb.Append(symbols[i]);
            }
        }

        return sb.ToString();
    }
}