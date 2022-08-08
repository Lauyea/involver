using System.Text;

namespace Involver.Helpers
{
    public static class DiceHelper
    {
        public static bool ReplaceRollDiceString(ref string strToDice)
        {
            Random random = new();
            int start = 0;
            int at = 0;
            int end = strToDice.Length;
            int count;
            int diceValue;
            bool hasChange = false;

            while ((start <= end) && (at > -1))
            {
                end = strToDice.Length;
                diceValue = 0;
                count = end - start;
                at = strToDice.IndexOf("Dice", start, count);
                //-1 等於沒找到
                if (at == -1) break;
                //Dice05D10，D 在index = 6
                if (strToDice[at + 6] == 'D')
                {
                    hasChange = true;
                    int rollTimes = int.Parse(strToDice[at + 4].ToString() + strToDice[at + 5].ToString());
                    int diceSides = int.Parse(strToDice[at + 7].ToString() + strToDice[at + 8].ToString());

                    int iterativeTimes = rollTimes;

                    while (iterativeTimes > 0)
                    {
                        diceValue += random.Next(1, diceSides + 1);
                        iterativeTimes--;
                    }

                    string strToBeChanged;
                    string changingStr;
                    if (rollTimes < 10 && diceSides < 10)
                    {
                        strToBeChanged = "Dice0" + rollTimes + "D0" + diceSides;
                        changingStr = rollTimes + "D0" + diceSides + ": " + diceValue;
                    }
                    else if (rollTimes < 10)
                    {
                        strToBeChanged = "Dice0" + rollTimes + "D" + diceSides;
                        changingStr = rollTimes + "D" + diceSides + ": " + diceValue;
                    }
                    else if (diceSides < 10)
                    {
                        strToBeChanged = "Dice" + rollTimes + "D0" + diceSides;
                        changingStr = rollTimes + "D0" + diceSides + ": " + diceValue;
                    }
                    else
                    {
                        strToBeChanged = "Dice" + rollTimes + "D" + diceSides;
                        changingStr = rollTimes + "D" + diceSides + ": " + diceValue;
                    }
                    StringBuilder stringBuilder = new StringBuilder(strToDice);
                    stringBuilder.Replace(strToBeChanged, changingStr, at, strToBeChanged.Length);
                    strToDice = stringBuilder.ToString();
                }
                //因為StringBuilder 可以分段改字串了，而且Roll也不見了，就不需要改start 的位置了(X)
                //start變動，不用每次都從頭算，剩下需要計算的位數會變少，增加效率

                // Dice has 4 digits
                start = at + 5;
            }

            return hasChange;
        }
    }
}
