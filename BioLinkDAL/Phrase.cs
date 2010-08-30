﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioLink.Data.Model {

    public class PhraseCategory : BiolinkDataObject {

        public int PhraseCatID { get; set; }

        public string Category { get; set; }

        public bool Fixed { get; set; }

    }

    public class Phrase : BiolinkDataObject {

        public int PhraseID { get; set; }

        public int PhraseCatID { get; set; }

        public string PhraseText { get; set; }
    }

}
