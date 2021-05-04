using Domain.ACCUpdatesStructs;
using Domain.Enums;
using System;
using System.Linq;

namespace Domain.Models {
    public class LapModel {

        public int? LaptimeMS { get; private set; }
        public string LaptimeString { get; private set; }
        public int? Split1MS { get; private set; }
        public string Split1String { get; private set; }
        public int? Split2MS { get; private set; }
        public string Split2String { get; private set; }
        public int? Split3MS { get; private set; }
        public string Split3String { get; private set; }
        public LapType Type { get; private set; }
        public bool IsValid { get; private set; }
        public string LapHint { get; private set; }

        public void Update(LapInfo lapUpdate) {
            var isChanged = LaptimeMS != lapUpdate.LaptimeMS;
            if (isChanged) {
                LaptimeMS = lapUpdate.LaptimeMS;
                if (LaptimeMS == null)
                    LaptimeString = "--";
                else
                    LaptimeString = $"{TimeSpan.FromMilliseconds(LaptimeMS.Value):mm\\:ss\\.fff}";

                Split1MS = lapUpdate.Splits.FirstOrDefault();
                if (Split1MS != null)
                    Split1String = $"{TimeSpan.FromMilliseconds(Split1MS.Value):ss\\.f}";
                else
                    Split1String = "";

                Split2MS = lapUpdate.Splits.Skip(1).FirstOrDefault();
                if (Split2MS != null)
                    Split2String = $"{TimeSpan.FromMilliseconds(Split2MS.Value):ss\\.f}";
                else
                    Split2String = "";

                Split3MS = lapUpdate.Splits.Skip(2).FirstOrDefault();
                if (Split3MS != null)
                    Split3String = $"{TimeSpan.FromMilliseconds(Split3MS.Value):ss\\.f}";
                else
                    Split3String = "";

                Type = lapUpdate.Type;
                IsValid = lapUpdate.IsValidForBest;

                if (Type == LapType.Outlap)
                    LapHint = "OUT";
                else if (Type == LapType.Inlap)
                    LapHint = "IN";
                else
                    LapHint = "";
            }
        }
    }
}
