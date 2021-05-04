using System.Collections.Generic;

namespace Domain.Models {
    public class DirectorTipModel {
        public TipModel<CarUpdateModel> CarTip { get; set; }
        public List<TipModel<CameraModel>> CamTips { get; set; }
    }
}
