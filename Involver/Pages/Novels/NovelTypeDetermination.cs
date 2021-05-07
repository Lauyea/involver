using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Involver.Pages.Novels
{
    public class NovelTypeDetermination
    {
        public string GetTypeName(Models.NovelModel.Type? type)
        {
            string TypeName = "";
            switch (type)
            {
                case (Models.NovelModel.Type.Fantasy):
                    TypeName = "奇幻";
                    break;
                case (Models.NovelModel.Type.History):
                    TypeName = "歷史";
                    break;
                case (Models.NovelModel.Type.Love):
                    TypeName = "愛情";
                    break;
                case (Models.NovelModel.Type.Real):
                    TypeName = "真實";
                    break;
                case (Models.NovelModel.Type.Modern):
                    TypeName = "現代";
                    break;
                case (Models.NovelModel.Type.Science):
                    TypeName = "科幻";
                    break;
                case (Models.NovelModel.Type.Horror):
                    TypeName = "驚悚";
                    break;
                case (Models.NovelModel.Type.Detective):
                    TypeName = "推理";
                    break;
                default:
                    break;
            }
            return TypeName;
        }
    }
}
