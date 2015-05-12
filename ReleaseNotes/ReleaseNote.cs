using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReleaseNotes
{
    public class ReleaseNote
    {
        public string WorkItemAra { get; set; }
        public string BuildNumber { get; set; }
        public int WorkItemId { get; set; }
        public string WorkItemTitle { get; set; }

        public string WorkItemType { get; set; } // Completed User Story or Resolved Bug
        public string UserStoryId { get; set; }
        public string UserStoryTitle { get; set; }



    }

    public class UserStory
    {
        public string BuildNo { get; set; }
        public WorkItem UserStoryWorkItem { get; set; }
        public IList<WorkItem> Tasks { get; set; }
        public IList<WorkItem> Bugs { get; set; }
    }
}
