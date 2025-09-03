using System.Text;

namespace Involver.Helpers
{
    public static class DiceHelper
    {
        /// <summary>
        /// 取代留言內容中的擲骰指令字串，並回傳成功取代的數量。
        /// </summary>
        /// <param name="strToDice">要處理的留言內容字串。</param>
        /// <returns>成功取代的指令數量。</returns>
        public static int ReplaceRollDiceString(ref string strToDice)
        {
            Random random = new();
            int start = 0;
            int replacements = 0;

            // 使用 while 迴圈來尋找所有 "Dice" 關鍵字
            while ((start < strToDice.Length) && (start = strToDice.IndexOf("Dice", start)) != -1)
            {
                // 檢查格式是否為 DiceXXDYY (共9個字元)
                if (strToDice.Length >= start + 9 && strToDice[start + 6] == 'D')
                {
                    // 嘗試解析擲骰次數與骰子面數
                    if (int.TryParse(strToDice.Substring(start + 4, 2), out int rollTimes) &&
                        int.TryParse(strToDice.Substring(start + 7, 2), out int diceSides))
                    {
                        // 確保數值有效
                        if (rollTimes > 0 && diceSides > 0)
                        {
                            int diceValue = 0;
                            for (int i = 0; i < rollTimes; i++)
                            {
                                diceValue += random.Next(1, diceSides + 1);
                            }

                            string strToBeChanged = strToDice.Substring(start, 9);
                            string changingStr = $"{rollTimes:D1}D{diceSides:D2}: {diceValue}";

                            // 使用 StringBuilder 以提升效率
                            var stringBuilder = new StringBuilder(strToDice);
                            stringBuilder.Replace(strToBeChanged, changingStr, start, strToBeChanged.Length);
                            strToDice = stringBuilder.ToString();

                            replacements++;
                            // 從取代後字串的結尾繼續搜尋
                            start += changingStr.Length;
                        }
                        else
                        {
                            // 如果 rollTimes 或 diceSides 為 0，則跳過此 "Dice"
                            start++;
                        }
                    }
                    else
                    {
                        // 解析數字失敗，跳過
                        start++;
                    }
                }
                else
                {
                    // 格式不符，跳過
                    start++;
                }
            }

            return replacements;
        }
    }
}
