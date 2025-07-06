#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using FTOptix.HMIProject;
using FTOptix.Report;
using FTOptix.NetLogic;
using FTOptix.WebUI;
using FTOptix.Recipe;
using FTOptix.RAEtherNetIP;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.Alarm;
using FTOptix.EventLogger;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.CommunicationDriver;
using FTOptix.Core;
using FTOptix.TwinCAT;
using FTOptix.OPCUAServer;
using static RT_CoT_Helper;
#endregion

public class RT_CoT_CTRL_CommentArea : BaseNetLogic
{
    private IUANode _content;
    private NodeId _currentuserRole;
    private IUAVariable _numberOfComments;
    private CoT_Comment _newComment;
    private string _objectIdentifier;
 
    private Store _db;
    private string _commentsTableName = "DBComments";

    public override void Start()
    {
        try
        {
            _numberOfComments = GetVariable("numberOfComments",LogicObject);
            _content = GetPointer("content", LogicObject).GetPointedObj<IUANode>();
            _objectIdentifier = GetVariable("objectIdentifier", LogicObject).GetVariableValue<string>();
            _currentuserRole = GetVariable("userRole", LogicObject).GetVariableValue<NodeId>();
            _db = GetPointer("dataBase", LogicObject).GetPointedObj<Store>();
           
            createCommentsData(_db, $"SELECT * FROM {_commentsTableName} ORDER BY updatedAt ASC");
        }
        catch (Exception ex)
        {
            Log.Error(Owner.BrowseName, $"Start method failed: {ex.Message}");
        }
    }

    private void createCommentsData(Store db, string query)
    {
        try
        {
            if(string.IsNullOrEmpty(_objectIdentifier))
            {   
                return;
            }

            db.Query(query, out string[] header, out object[,] resultSet);

            int objectIdentifierIndex = Array.FindIndex(header, a => a == "objectIdentifier");
            int commentIndex = Array.FindIndex(header, a => a == "comment");
            int commentIdIndex = Array.FindIndex(header, a => a == "commentId");

            if (commentIndex == -1|| objectIdentifierIndex== -1 || commentIdIndex == -1)
            {
                Log.Error(Owner.BrowseName, $"In Database: {_db.BrowseName} the table {_commentsTableName} is not meant for comments.");
                return;
            }
          

            for (int row = 0; row < resultSet.GetLength(0); row++)
            {
                string _id = resultSet[row, objectIdentifierIndex].ToString();
                string _comment = resultSet[row, commentIndex].ToString();
                string _commentId = resultSet[row, commentIdIndex].ToString();

                
                if (!(_id== _objectIdentifier))
                {   
                    continue;
                }

                var _oldcomment = InformationModel.Make<CoT_Comment>($"{row + 1}");
   
                GetVariable("userRole" , _oldcomment).Value = _currentuserRole;
                GetVariable("text" , _oldcomment).Value = _comment;

                var textVariable= GetVariable("text", _oldcomment);
               
                textVariable.VariableChange += (sender, e) =>
                {
                    OnCommentTextChanged(sender, e, _commentId);
                };
                 var _deleteCommentVariable = GetVariable("deletePressed", _oldcomment);
                _deleteCommentVariable.VariableChange += (sender, e) =>
                {
                    OnDeletePressedWithInfo(sender, e, _commentId);
                };

                _content.Add(_oldcomment);
                _numberOfComments.Value++;
                
            }

        }
        catch (Exception ex)
        {
            Log.Error(Owner.BrowseName, $"createCommentsData failed: {ex.Message}");
        }
    }

    [ExportMethod]
    public void createComment(string comment)
    {
        try
        {
            Guid g = Guid.NewGuid();
            string _commentId = g.ToString();
            DateTime _currenttime = DateTime.UtcNow;
            var myTable = _db.Tables.Get<Table>(_commentsTableName);
            string[] columns = {"objectIdentifier", "comment", "commentId", "updatedAt"};
            var values = new object[1, columns.Length];

            values[0, 0] = _objectIdentifier;
            values[0, 1] = comment;
            values[0, 2] = _commentId;
            values[0, 3] = _currenttime;

            myTable.Insert(columns, values);
            Log.Info("InsertData", "Inserting data for user " + values[0, 0]);

            _newComment = InformationModel.Make<CoT_Comment>("comment");
            GetVariable("userRole" , _newComment).Value = _currentuserRole;
            GetVariable("text" , _newComment).Value = comment;

            var _textVariable = GetVariable("text", _newComment);
            _textVariable.VariableChange += (sender, e) =>
            {
                OnCommentTextChanged(sender, e, _commentId);
            };


            var _deleteCommentVariable = GetVariable("deletePressed", _newComment);
            _deleteCommentVariable.VariableChange += (sender, e) =>
            {
                OnDeletePressedWithInfo(sender, e, _commentId);
            };

            _content.Add(_newComment);
            _numberOfComments.Value++;
        
        }
        catch (Exception ex)
        {
            Log.Error("CommentArea", $"createComment failed: {ex.Message}");
        }
    }

    private void OnDeletePressedWithInfo(object sender, VariableChangeEventArgs e, string commentId)
    {
        try
        {
            if (e.NewValue != null && (bool)e.NewValue)
            {
                if (string.IsNullOrEmpty(commentId))
                {
                    Log.Error("CommentArea", "Missing commentId during delete event.");
                    return;
                }

                string deleteQuery = $"DELETE FROM {_commentsTableName} WHERE commentId = '{commentId}'";
                _db.Query(deleteQuery, out string[] header, out object[,] resultSet);

                var deleteVar = sender as IUAVariable;
                var commentNode = deleteVar?.Parent;
                commentNode?.Delete();

                _numberOfComments.Value--;
    
            }
        }
        catch (Exception ex)
        {
            Log.Error("CommentArea", $"OnDeletePressedWithInfo failed: {ex.Message}");
        }
    }

    private void OnCommentTextChanged(object sender, VariableChangeEventArgs e, string commentId)
    {
        try
        {
            if (e.NewValue == null || string.IsNullOrEmpty(commentId))
            {
                Log.Error("CommentArea", "Invalid comment ID or new text value.");
                return;
            }

            string newText = e.NewValue.Value?.ToString() ?? string.Empty;
            string updateQuery = $"UPDATE {_commentsTableName} SET comment = '{newText.Replace("'", "''")}' WHERE commentId = '{commentId}'";
            _db.Query(updateQuery, out string[] header, out object[,] resultSet);
                
        
        }
        catch (Exception ex)
        {
            Log.Error("CommentArea", $"OnCommentTextChanged failed: {ex.Message}");
        }
    }
}
