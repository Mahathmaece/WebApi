
using System;
using System.Text.RegularExpressions;

namespace WebApi.Helpers
{
    public class Validation
    {
        // contains special character tab (\t) or the literal string \t
        public static bool HasTab(string text)
        {
            Regex regex = new Regex(@"[\t]+", RegexOptions.Multiline);
            Match match = regex.Match(text);
            return match.Success || text.Contains("\\t");
        }

        // based on SAP or AMS max length. Allow null/empty values
        public static bool ValidLength(string text, int maxLength)
        {
            return text == null || text.Length <= maxLength;
        }

        public static bool ValidLicensePlateNumber(string text)
        {
            // should not contain tab (\t) and within SAP max length
            return (!HasTab(text)) && ValidLength(text, 15);
        }

        public static bool ValidRoom(string text)
        {
            // should not contain tab (\t) and within SAP max length
            return (!HasTab(text)) && ValidLength(text, 8);
        }

        //public static bool ValidEvaluationGroup1(string text)
        //{
        //    // should not contain tab (\t) and within SAP max length
        //    return (!HasTab(text)) && ValidLength(text, 4);
        //}

        //public static bool ValidEvaluationGroup4(string text)
        //{
        //    // should not contain tab (\t) and within SAP max length
        //    return (!HasTab(text)) && ValidLength(text, 4);
        //}

        public static bool ValidInventoryNote(string text)
        {
            // should not contain tab (\t) and within SAP max length
            return (!HasTab(text)) && ValidLength(text, 15);
        }
    }
}
