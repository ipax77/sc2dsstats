using System.Collections.Generic;

namespace sc2dsstats.decode.Models
{
    public class s2replay
    {
        public int ID { get; set; }
        public string REPLAY { get; set; }
        public double GAMETIME { get; set; }
        public List<s2player> PLAYERS { get; set; } = new List<s2player>();
    }

    public class s2player
    {
        public string NAME { get; set; }
        public int POS { get; set; }
        //public s2init _init { get; set; } = new s2init();
        public int TEAM { get; set; }
        public string RACE { get; set; }
        public int RESULT { get; set; }


        public s2player()
        {

        }

        public s2player(string name)
        {
            NAME = name;
        }
    }

    public class s2init
    {
        public int Team { get; set; }
        public int UserID { get; set; }
        public int SlotID { get; set; }
        public int Race { get; set; }
    }
}
