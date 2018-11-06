﻿using System;
using System.Collections.Generic;
using System.Text;

namespace InternetHospital.DataAccess.Entities 
{
    public class FeedBack 
    {
        public int Id { set; get; }
        public string Text { set; get; }
        public int TypeId { set; get; }
        public virtual FeedBackType Type { set; get; }
        public int FeedBackId { set; get; }
        public virtual User User { set; get; }
        public int UserId { set; get; }
        public DateTime DateTime { set; get; }
    }
}
