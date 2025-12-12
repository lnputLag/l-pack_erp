using System;
using System.Globalization;
using System.Windows.Media;

namespace Client.Common
{
    /// <summary>
    /// пополнительные функции для работы с символами
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2022-07-07</released>
    /// <changed>2022-07-07</changed>
    public static class CharExtension
    {
        /// <summary>
        /// проверяет, что символ кириллический
        /// </summary>
        /// <returns></returns>
        public static Boolean IsCyrillic(this Char c)
        {
            bool result = false;

            { 
                if(
                       c=='а' || c=='А'
                    || c=='б' || c=='Б'
                    || c=='в' || c=='В'
                    || c=='г' || c=='Г'
                    || c=='е' || c=='Е'
                    || c=='ё' || c=='Ё'
                    || c=='ж' || c=='Ж'
                    || c=='з' || c=='З'
                    || c=='и' || c=='И'
                    || c=='й' || c=='Й'
                    || c=='к' || c=='К'
                    || c=='л' || c=='Л'
                    || c=='м' || c=='М'
                    || c=='н' || c=='Н'
                    || c=='о' || c=='О'
                    || c=='п' || c=='П'
                    || c=='р' || c=='Р'
                    || c=='с' || c=='С'
                    || c=='т' || c=='Т'
                    || c=='у' || c=='У'
                    || c=='ф' || c=='Ф'
                    || c=='х' || c=='Х'
                    || c=='ц' || c=='Ц'
                    || c=='ч' || c=='Ч'
                    || c=='ш' || c=='Ш'
                    || c=='щ' || c=='Щ'
                    || c=='ъ' || c=='Ъ'
                    || c=='ы' || c=='Ы'
                    || c=='ь' || c=='Ь'
                    || c=='э' || c=='Э'
                    || c=='ю' || c=='Ю'
                    || c=='я' || c=='Я'
                )
                {
                    result=true;
                }

            }

            return result;
        }

        /// <summary>
        /// проверяет, что символ кириллический
        /// </summary>
        /// <returns></returns>
        public static Boolean IsLatin(this Char c)
        {
            bool result = false;

            { 
                if(
                       c=='a' || c=='A' 
                    || c=='b' || c=='B'
                    || c=='c' || c=='C'
                    || c=='d' || c=='D'
                    || c=='e' || c=='E'
                    || c=='f' || c=='F'
                    || c=='g' || c=='G'
                    || c=='h' || c=='H'
                    || c=='i' || c=='I'
                    || c=='j' || c=='J'
                    || c=='k' || c=='K'
                    || c=='l' || c=='L'
                    || c=='m' || c=='M'
                    || c=='n' || c=='N'
                    || c=='o' || c=='O'
                    || c=='p' || c=='P'
                    || c=='q' || c=='Q'
                    || c=='r' || c=='R'
                    || c=='s' || c=='S'
                    || c=='t' || c=='T'
                    || c=='u' || c=='U'
                    || c=='v' || c=='V'
                    || c=='w' || c=='W'
                    || c=='x' || c=='X'
                    || c=='y' || c=='Y'
                    || c=='z' || c=='Z'
                )
                {
                    result=true;
                }

            }

            return result;
        }

        

    }
}
