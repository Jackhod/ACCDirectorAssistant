using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Enums {
	public enum SessionPhase {
		NONE = 0,
		Starting = 1,
		PreFormation = 2,
		FormationLap = 3,
		PreSession = 4,
		Session = 5,
		SessionOver = 6,
		PostSession = 7,
		ResultUI = 8
	};
}
