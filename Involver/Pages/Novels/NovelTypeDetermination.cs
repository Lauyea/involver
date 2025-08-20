using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccess.Common;
using Type = DataAccess.Common.Type;

namespace Involver.Pages.Novels
{
    public class NovelTypeDetermination
    {
        public string GetTypeName(Type? type)
        {
            string TypeName = "";
            switch (type)
            {
                case (Type.Fantasy):
                    TypeName = "奇幻";
                    break;
                case (Type.History):
                    TypeName = "歷史";
                    break;
                case (Type.Love):
                    TypeName = "愛情";
                    break;
                case (Type.Real):
                    TypeName = "真實";
                    break;
                case (Type.Modern):
                    TypeName = "現代";
                    break;
                case (Type.Science):
                    TypeName = "科幻";
                    break;
                case (Type.Horror):
                    TypeName = "驚悚";
                    break;
                case (Type.Detective):
                    TypeName = "推理";
                    break;
                default:
                    break;
            }
            return TypeName;
        }
    }
}
