using System;

namespace Cytoid
{
    [Serializable]
    public class Chart
    {
        public int format_version;
        public int time_base;
        public int start_offset_time;
        public Page[] page_list;
        public Tempo[] tempo_list;
        public EventOrder[] event_order_list;
        public Note[] note_List;
    }
}