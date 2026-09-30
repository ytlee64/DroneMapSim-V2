using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;


namespace ExcelToCode
{
    public static class StringHelpers
    {
        // 구분자(공백, 하이픈, 언더스코어, 점 등)로 단어를 나누어 PascalCase 생성
        public static string ToPascalCase(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // 공백/특수문자 연속을 구분자로 처리
            var parts = Regex.Split(input.Trim(), @"[^A-Za-z0-9]+")
                             .Where(p => !string.IsNullOrEmpty(p))
                             .ToArray();

            if (parts.Length == 0)
                return string.Empty;

            TextInfo ti = CultureInfo.InvariantCulture.TextInfo;
            return string.Concat(parts.Select(p => ti.ToTitleCase(p.ToLowerInvariant())));
        }

        // lowerCamel (첫 문자 소문자) 생성
        public static string ToCamelCase(string? input)
        {
            var pascal = ToPascalCase(input);
            if (string.IsNullOrEmpty(pascal))
                return pascal;
            return char.ToLowerInvariant(pascal[0]) + pascal.Substring(1);
        }

        // 기본 전처리: 따옴표/제어문자 제거, 줄바꿈 통일, 앞뒤 공백 제거
        public static string Sanitize(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            return input
                .Replace("\"", "'")
                .Replace("U+202C", "")
                .Replace("\r\n", "|")
                .Replace("\r", "|")
                .Replace("\n", "|")
                .Trim();
        }

        // 정상 숫자(십진법 혹은 0x 헥스)인지 검사하고 정규화된 문자열을 반환 (Invariant 표기)
        public static bool TryNormalizeNumber(string? input, out string normalizedInvariant)
        {
            normalizedInvariant = string.Empty;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            string s = Regex.Replace(input.Trim(), @"\s+", "");
            s = s.Replace("_", ""); // 허용되는 구분자 제거

            // 부호 처리
            bool negative = false;
            if (s.StartsWith("+"))
                s = s.Substring(1);
            else if (s.StartsWith("-"))
            {
                negative = true;
                s = s.Substring(1);
            }

            // 헥스 형식 확인: 0x... 또는 0X...
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                string hexDigits = s.Substring(2);
                if (ulong.TryParse(hexDigits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var ulongVal))
                {
                    // 반환은 0xHEX (대문자 HEX로 표현), 부호가 있으면 앞에 '-'
                    string hex = "0x" + ulongVal.ToString("X");
                    normalizedInvariant = negative ? "-" + hex : hex;
                    return true;
                }
                return false;
            }

            // 쉼표를 소수점으로 대체 후 실수 파싱 시도
            string dec = s.Replace(',', '.');
            if (double.TryParse(dec, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsed))
            {
                if (negative) parsed = -parsed;
                normalizedInvariant = parsed.ToString("G", CultureInfo.InvariantCulture);
                return true;
            }

            return false;
        }

        // 파싱에 실패하면 ArgumentException을 던집니다.
        public static string NormalizeNumber(string input)
        {
            if (TryNormalizeNumber(input, out var norm))
                return norm;
            return "0";
        }

        // --- 배열 표기 검사 및 파싱 유틸 ---
        // 허용 예: "array[10]", "Array[ 10 ]", "MY_ARRAY[3]"
        // 반환: baseName (대괄호 제거된 이름, 트림됨), count (파싱된 정수 > 0)
        private static readonly Regex ArrayNotationRegex = new Regex(@"^\s*(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\[\s*(?<count>\d+)\s*\]\s*$", RegexOptions.Compiled);

        // name이 배열 표기인지 검사하고, 맞으면 baseName과 count를 반환
        public static bool TryParseArrayNotation(string? input, out string baseName, out int count)
        {
            baseName = string.Empty;
            count = 1;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            var m = ArrayNotationRegex.Match(input.Trim());
            if (!m.Success)
                return false;

            baseName = m.Groups["name"].Value.Trim();
            if (int.TryParse(m.Groups["count"].Value, out int parsed) && parsed >= 0)
            {
                count = parsed;
                return true;
            }

            return false;
        }


    }
}
