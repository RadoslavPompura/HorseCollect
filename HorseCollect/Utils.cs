using System;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Security.Cryptography;

namespace HorseCollect
{
    public class Utils
    {
        //public static object lockObj;
        private static NumberStyles style = NumberStyles.Number | NumberStyles.AllowCurrencySymbol | NumberStyles.AllowDecimalPoint;
        private static CultureInfo culture = CultureInfo.CreateSpecificCulture("en-GB");

        public static double ParseToDouble(string str)
        {
            double value = 0;
            double.TryParse(str, style, culture, out value);
            return value;
        }

        public static int ParseToInt(string str)
        {
            int val = 0;
            int.TryParse(str, out val);
            return val;
        }

        public static string AddZeroLetter(int value)
        {
            if (value < 10) return string.Format("0{0}", value);
            else return value.ToString();
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public static long getTick()
        {
            TimeSpan t = (DateTime.UtcNow - new DateTime(1970, 1, 1));
            long timestamp = (long)t.TotalMilliseconds;
            return timestamp;
        }

        public static long getUKTick()
        {
            TimeSpan t = (DateTime.Now - new DateTime(1970, 1, 1));
            long timestamp = (long)t.TotalMilliseconds;
            return timestamp;
        }

        public static bool TryParseGuid(string guidString, out Guid guid)
        {
            if (guidString == null) throw new ArgumentNullException("guidString");
            try
            {
                guid = new Guid(guidString);
                return true;
            }
            catch (FormatException)
            {
                guid = default(Guid);
                return false;
            }
        }

        public static string ToFractions(double number, int precision = 100)
        {
            int w, n, d;
            number = number - 1;
            Utils.RoundToMixedFraction(number, precision, out w, out n, out d);
            var ret = $"{w*d + n}/{d}";
            return ret;
        }
        public static double FractionToDouble(string fraction)
        {
            double result;

            if (double.TryParse(fraction, out result))
            {
                return result;
            }

            string[] split = fraction.Split(new char[] { ' ', '/' });

            if (split.Length == 2 || split.Length == 3)
            {
                int a, b;

                if (int.TryParse(split[0], out a) && int.TryParse(split[1], out b))
                {
                    if (split.Length == 2)
                    {
                        return 1 + Math.Floor((double)100 * a / b) / 100;
                    }

                    int c;

                    if (int.TryParse(split[2], out c))
                    {
                        return a + (double)b / c;
                    }
                }
            }

            throw new FormatException("Not a valid fraction. => " + fraction);
        }

        public static double calculateValuePercent(double softOdds, double baseOdds1, double baseOdds2, ref string logText)
        {
            double impliedProb1 = 1 / baseOdds1;
            double impliedProb2 = 1 / baseOdds2;
            double margin = impliedProb1 + impliedProb2 - 1;
            double trueProb1 = impliedProb1 / (1 + margin);
            double trueProb2 = impliedProb2 / (1 + margin);
            double fairOdds1 = 1 / trueProb1;
            double fairOdds2 = 1 / trueProb2;
            double valuePercent = 0;
            valuePercent = (softOdds - fairOdds1) / fairOdds1 * 100;
            valuePercent = Math.Floor(valuePercent * 100) / 100;
            logText = string.Format("BaseOdds: {0}, Margin: {1}, FairOdds: {2}, Value: {3}",
                baseOdds1.ToString("N2"),
                (margin * 100).ToString("N2"),
                fairOdds1.ToString("N2"),
                valuePercent.ToString("N2"));
            // Filter if margin is negative.
            if (margin < 0) return -100;
            return valuePercent;
        }

        public static string Between(string STR, string FirstString, string LastString = null)
        {
            string FinalString;
            int Pos1 = STR.IndexOf(FirstString) + FirstString.Length;
            if (LastString != null)
            {
                STR = STR.Substring(Pos1);
                int Pos2 = STR.IndexOf(LastString);
                if (Pos2 > 0)
                    FinalString = STR.Substring(0, Pos2);
                else
                    FinalString = STR;
            }
            else
            {
                FinalString = STR.Substring(Pos1);
            }

            return FinalString;
        }

        public static string ReplaceStr(string STR, string ReplaceSTR, string FirstString, string LastString)
        {
            string FinalString;
            int Pos1 = STR.IndexOf(FirstString) + FirstString.Length;
            if (LastString != null)
            {
                string preSTR = STR.Substring(0, Pos1);
                int Pos2 = STR.IndexOf(LastString, Pos1);
                FinalString = preSTR + ReplaceSTR + STR.Substring(Pos2);
            }
            else
            {
                string preSTR = STR.Substring(0, Pos1);
                FinalString = preSTR + ReplaceSTR;
            }

            return FinalString;
        }

        public static void RoundToMixedFraction(double input, int accuracy, out int whole, out int numerator, out int denominator)
        {
            double dblAccuracy = (double)accuracy;
            whole = (int)(Math.Truncate(input));
            var fraction = Math.Abs(input - whole);
            if (fraction == 0)
            {
                numerator = 0;
                denominator = 1;
                return;
            }
            var n = Enumerable.Range(0, accuracy + 1).SkipWhile(e => (e / dblAccuracy) < fraction).First();
            var hi = n / dblAccuracy;
            var lo = (n - 1) / dblAccuracy;
            if ((fraction - lo) < (hi - fraction)) n--;
            if (n == accuracy)
            {
                whole++;
                numerator = 0;
                denominator = 1;
                return;
            }
            var gcd = Utils.GCD(n, accuracy);
            numerator = n / gcd;
            denominator = accuracy / gcd;
        }

        public static int GCD(int a, int b)
        {
            if (b == 0) return a;
            else return Utils.GCD(b, a % b);
        }

        public static bool CheckBetweenCurrentTime(string timeStr)
        {
            bool res_ = false;
            try
            {
                double siteTime_mins = TimeSpan.Parse(timeStr).TotalMinutes;
                TimeSpan currentTime = getCurrentUKTime().TimeOfDay;
                double curTime_mins = currentTime.TotalMinutes;

                if (siteTime_mins <= curTime_mins)
                    return false;

                if (siteTime_mins - curTime_mins < 5)
                    return false;

                return true;
            }
            catch (Exception ee)
            {

            }
            return res_;
        }

        public static DateTime getCurrentUKTime()
        {
            var britishZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
            DateTime newDate = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.Local, britishZone);

            return newDate;
        }

        public static string Get_HASH_SHA512(string password, string username, byte[] salt)
        {
            try
            {
                //required NameSpace: using System.Text;
                //Plain Text in Byte
                byte[] plainTextBytes = Encoding.UTF8.GetBytes(password + username);

                //Plain Text + SALT Key in Byte
                byte[] plainTextWithSaltBytes = new byte[plainTextBytes.Length + salt.Length];

                for (int i = 0; i < plainTextBytes.Length; i++)
                {
                    plainTextWithSaltBytes[i] = plainTextBytes[i];
                }

                for (int i = 0; i < salt.Length; i++)
                {
                    plainTextWithSaltBytes[plainTextBytes.Length + i] = salt[i];
                }

                HashAlgorithm hash = new SHA512Managed();
                byte[] hashBytes = hash.ComputeHash(plainTextWithSaltBytes);
                byte[] hashWithSaltBytes = new byte[hashBytes.Length + salt.Length];

                for (int i = 0; i < hashBytes.Length; i++)
                {
                    hashWithSaltBytes[i] = hashBytes[i];
                }

                for (int i = 0; i < salt.Length; i++)
                {
                    hashWithSaltBytes[hashBytes.Length + i] = salt[i];
                }

                return Convert.ToBase64String(hashWithSaltBytes);
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string getTimeFormat(int totalMin)
        {
            string timeFormat = "";
            try
            {
                int hh = totalMin / 60;
                int mm = totalMin % 60;
                if (mm < 10)
                {
                    timeFormat = hh.ToString() + ":0" + mm.ToString();
                }
                else
                {
                    timeFormat = hh.ToString() + ":" + mm.ToString();
                }
                return timeFormat;
            }
            catch (Exception ex)
            {
                return timeFormat;
            }
        }
    }
}
