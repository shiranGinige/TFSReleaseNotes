using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System.Configuration;

namespace ReleaseNotes
{
    public class ReleaseNoteGenerator
    {

        private TfsTeamProjectCollection _tfs;
        private string _selectedTeamProject;
        private IBuildServer _bs;
        private WorkItemStore _wis;


        private IBuildDetail[] BuildDetails(string buildName)
        {
            var test = _bs.CreateBuildDetailSpec(_selectedTeamProject);
            int noRequeted; // set to 1 incase no value passed
            test.MaxBuildsPerDefinition = 1;//(int.TryParse(txtNoOfBuilds.Text.ToString(), out noRequeted)) ? noRequeted : 1;
            test.QueryOrder = BuildQueryOrder.FinishTimeDescending;
            test.DefinitionSpec.Name = buildName;
          //  test.Status = chkOnlySuccessful.Checked ? BuildStatus.Succeeded : BuildStatus.All;
            var builds = _bs.QueryBuilds(test).Builds;
            return builds;
        }

        private IBuildDefinition[] GetAllBuildDefinitionsFromTheTeamProject()
        {
            _bs = _tfs.GetService<IBuildServer>();
            return _bs.QueryBuildDefinitions(_selectedTeamProject);
        }
        private void ConnectToTfsAndPickAProject()
        {
            this._tfs = new TfsTeamProjectCollection(new Uri(ConfigurationManager.AppSettings["TFSUrl"])); // Make configurable 
            this._selectedTeamProject = ConfigurationManager.AppSettings["TeamProjectName"];
        }

          public void GenerateDocuments()
        {
            ConnectToTfsAndPickAProject();
            GetAllBuildDefinitionsFromTheTeamProject();

            _wis = _tfs.GetService<WorkItemStore>();

            var releaseNotes = new List<ReleaseNote>();
            var didntMakeItToTheReleaseNotes = new List<ReleaseNote>();
            var unionOfUserStories = new List<UserStory>();

            var builds = BuildDetails("BuildName"); 

            foreach (IBuildDetail build in builds)
            {
                List<IWorkItemSummary> wis = InformationNodeConverters.GetAssociatedWorkItems(build);
                var userStories = GetDistinctUserStoriesIncludedInTheBuild(wis, build.BuildNumber);

                foreach (var userStory in userStories)
                {
                    if (
                        !unionOfUserStories.Select(a => a.UserStoryWorkItem.Id)
                            .Contains(userStory.UserStoryWorkItem.Id))
                    {
                        unionOfUserStories.Add(userStory);
                    }
                }
            }

            foreach (var userStory in unionOfUserStories)
            {
                if (userStory.Tasks.All(a => a.State == "Closed") && userStory.Bugs.Count == 0)
                {
                    if (!releaseNotes.Select(r => r.WorkItemId).Contains(userStory.UserStoryWorkItem.Id))
                        releaseNotes.Add(new ReleaseNote()
                        {
                            BuildNumber = userStory.BuildNo,
                            WorkItemId = userStory.UserStoryWorkItem.Id,
                            WorkItemTitle = userStory.UserStoryWorkItem.Title,
                            WorkItemType = "Completed User Story",
                            UserStoryId = "NA",
                            UserStoryTitle = "NA",
                            WorkItemAra = userStory.UserStoryWorkItem.AreaPath
                        });
                }

                else if (userStory.Bugs.Count > 0)
                {
                    foreach (var bug in userStory.Bugs)
                    {
                        if (bug.State == "Resolved" && !releaseNotes.Select(r => r.WorkItemId).Contains(bug.Id))
                        {
                            releaseNotes.Add(new ReleaseNote()
                            {
                                BuildNumber = userStory.BuildNo,
                                WorkItemId = bug.Id,
                                WorkItemTitle = bug.Title,
                                WorkItemType = "Resolved Bug",
                                UserStoryId = userStory.UserStoryWorkItem.Id.ToString(),
                                UserStoryTitle = userStory.UserStoryWorkItem.Title,
                                WorkItemAra = userStory.UserStoryWorkItem.AreaPath
                            });
                        }
                        else // TODO : Check this out. The bug id is 0 in this case ??
                        {
                            didntMakeItToTheReleaseNotes.Add(new ReleaseNote()
                            {
                                BuildNumber = userStory.BuildNo,
                                WorkItemId = bug.Id,
                                WorkItemTitle = bug.Title,
                                WorkItemType = "Unresolved Bug",
                                UserStoryId = userStory.UserStoryWorkItem.Id.ToString(),
                                UserStoryTitle = userStory.UserStoryWorkItem.Title,
                                WorkItemAra = userStory.UserStoryWorkItem.AreaPath
                            });
                        }
                    }
                }
                else
                {
                    if (!didntMakeItToTheReleaseNotes.Select(r => r.WorkItemId).Contains(userStory.UserStoryWorkItem.Id))
                        didntMakeItToTheReleaseNotes.Add(new ReleaseNote()
                        {
                            BuildNumber = userStory.BuildNo,
                            WorkItemId = userStory.UserStoryWorkItem.Id,
                            WorkItemTitle = userStory.UserStoryWorkItem.Title,
                            WorkItemType = "Incomplete User Story",
                            UserStoryId = userStory.UserStoryWorkItem.Id.ToString(),
                            UserStoryTitle = userStory.UserStoryWorkItem.Title,
                            WorkItemAra = userStory.UserStoryWorkItem.AreaPath
                        });
                }
            }


            //ReleaseNoteBindingSource.DataSource = releaseNotes;
            //this.reportViewer1.RefreshReport();
            GeneratePdfDocument(releaseNotes, "ReleaseNotes-" + DateTime.Today.ToShortDateString(), "Release Notes");
            GeneratePdfDocument(didntMakeItToTheReleaseNotes, "NonReleaseNotes-" + DateTime.Today.ToShortDateString(),
                "Checked In Stories - but incomplete");
        }

          private void GeneratePdfDocument(IList<ReleaseNote> releaseNotes, string fileName, string fileTitle)
          {
              fileName = fileName.Replace('/', '-');
              StringBuilder sb = new StringBuilder();

              sb.Append("<p><h1" + fileTitle + " | Generated on " + DateTime.Now.ToShortDateString() + " </h1></p>");


              using (StringWriter sw = new StringWriter())
              {
                  Table t = new Table() { };
                  t.Attributes.Add("border", "1");
                  TableHeaderRow headerRow = new TableHeaderRow() { };

                  headerRow.Cells.Add(new TableCell { Text = "Area", });
                  headerRow.Cells.Add(new TableCell { Text = "User Story Id", });
                  headerRow.Cells.Add(new TableCell { Text = "User Story Title", });
                  headerRow.Cells.Add(new TableCell { Text = "Work Item Id", });
                  headerRow.Cells.Add(new TableCell { Text = "Work Item", });
                  headerRow.Cells.Add(new TableCell { Text = "Build", });
                  headerRow.Cells.Add(new TableCell { Text = "Type", });




                  t.Rows.Add(headerRow);

                  foreach (var releaseNote in releaseNotes)
                  {
                      TableRow row = new TableRow() { };

                      row.Cells.Add(new TableCell { Text = releaseNote.WorkItemAra, });
                      row.Cells.Add(new TableCell { Text = releaseNote.UserStoryId, });
                      row.Cells.Add(new TableCell { Text = releaseNote.UserStoryTitle, });
                      row.Cells.Add(new TableCell { Text = "<a href='http://prodtfs02:8080/tfs/DefaultCollection/CSR/_workitems#_a=edit&id=" + releaseNote.WorkItemId + "'>" + releaseNote.WorkItemId + "</a>" });
                      row.Cells.Add(new TableCell { Text = releaseNote.WorkItemTitle, });
                      row.Cells.Add(new TableCell { Text = releaseNote.BuildNumber, });
                      row.Cells.Add(new TableCell { Text = releaseNote.WorkItemType, });
                      t.Rows.Add(row);
                  }


                  t.RenderControl(new HtmlTextWriter(sw));

                  string htmlTable = sw.ToString();
                  sb.Append(htmlTable);
              }


              using (StreamWriter sr = new StreamWriter(fileName + ".html"))
              {
                  sr.Write(sb.ToString());
              }
              //pdf.Save(fileName + ".html");
          }


          private IList<UserStory> GetDistinctUserStoriesIncludedInTheBuild(IList<IWorkItemSummary> workItemSummaries,
              string buildno)
          {
              IList<UserStory> userStoryList = new List<UserStory>();
              foreach (var wi in workItemSummaries)
              {
                  var w = _wis.GetWorkItem(wi.WorkItemId);
                  if (w.Type.Name == "Task" || w.Type.Name == "Bug")
                  {
                      var userStory = GetParentUserStory(w);
                      userStory.BuildNo = buildno;
                      if (!userStoryList.Select(a => a.UserStoryWorkItem.Id).Contains(userStory.UserStoryWorkItem.Id))
                      {
                          userStoryList.Add(userStory);
                      }
                      //userStoryList.Add(userStory);
                  }
              }

              return userStoryList;
          }


          private UserStory GetParentUserStory(WorkItem task)
          {
              if (task.WorkItemLinks != null)
              {
                  var firstItemInTheLinkedList = task.WorkItemLinks[0];
                  var parentWorkItem = _wis.GetWorkItem(firstItemInTheLinkedList.TargetId);

                  var userStory = new UserStory() { Tasks = new List<WorkItem>(), Bugs = new List<WorkItem>() };
                  userStory.UserStoryWorkItem = parentWorkItem;
                  foreach (WorkItemLink childItemLink in parentWorkItem.WorkItemLinks)
                  {
                      WorkItem childItem = _wis.GetWorkItem(childItemLink.TargetId);
                      if (childItem.Type.Name == "Bug")
                      {
                          userStory.Bugs.Add(childItem.Copy());
                      }
                      if (childItem.Type.Name == "Task")
                      {
                          userStory.Tasks.Add(childItem.Copy());
                      }
                  }
                  return userStory;
              }
              return null;
          }
    }


}
