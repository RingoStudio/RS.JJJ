using Newtonsoft.Json.Linq;
using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
using RS.Snail.JJJ.robot.include;
using RS.Tools.Common.Utils;
using RS.Tools.Excel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.modules
{
    /// <summary>
    /// 调查问卷模块
    /// </summary>
    internal class QuestionnaireM : IModule
    {
        #region FIELDS
        public string ModuleName { get => "QuestionnaireM"; }
        public Context _context { get; set; }

        private bool _inited = false;
        public bool Inited { get => _inited; }

        /// <summary>
        /// <question, <clone>>
        /// </summary>
        private dynamic _questionnaires;

        private object _answerLocker = new object();
        #endregion

        #region INIT
        public QuestionnaireM(boot.Context context)
        {
            this._context = context;
        }
        public void Init(bool load = false)
        {
            if (_inited) return;
            _inited = true;
            LoadCSV();
            RegistBackups();
        }
        /// <summary>
        /// 加载数据
        /// </summary>
        private void LoadCSV()
        {
            _questionnaires = new JObject();
            try
            {
                _questionnaires = IOHelper.GetCSV(Tools.Common.Enums.CSVType.RobotData, include.files.Questionnaire_CSV) ?? new JObject();
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, $"QuestionnaireM.LoadCSV");
            }
        }

        private void SaveCSV()
        {
            try
            {
                IOHelper.SaveCSV(Tools.Common.Enums.CSVType.RobotData, _questionnaires, include.files.Questionnaire_CSV);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, $"QuestionnaireM.SaveCSV");
            }

        }

        private void RegistBackups()
        {
            _context.BackupM.RegistSaveSessions(ModuleName, SaveCSV);
            _context.BackupM.RegistBackupSession(Tools.Common.Enums.CSVType.RobotData, include.files.Questionnaire_CSV);
        }
        #endregion

        #region METHODS
        /// <summary>
        /// 获取一个调查问题已有的数量
        /// </summary>
        /// <param name="question"></param>
        /// <returns></returns>
        public int QueryAnswerCount(string question)
        {
            if (_questionnaires[question] is null) return -1;
            return JSONHelper.GetCount(_questionnaires.Get(question));
        }
        /// <summary>
        /// 有没有人回复过
        /// </summary>
        /// <param name="question"></param>
        /// <returns></returns>
        public bool IsAnyAnswer(string question = "")
        {
            if (JSONHelper.GetCount(_questionnaires) == 0) return false;
            if (!string.IsNullOrEmpty(question)) return QueryAnswerCount(question) > 0;
            else
            {
                var keys = JSONHelper.GetKeys(_questionnaires);
                foreach (var key in keys)
                {
                    if (QueryAnswerCount(key) > 0) return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 建立新的调查问卷
        /// </summary>
        /// <param name="question"></param>
        public void SetQuestion(string question, string response = "")
        {
            _questionnaires[question] = new JObject();
            _questionnaires[question]["question"] = question;
            _questionnaires[question]["response"] = question;
        }
        /// <summary>
        /// 删除问题
        /// </summary>
        /// <param name="question"></param>
        public void DelQuestion(string question)
        {
            if (_questionnaires[question] is not null) _questionnaires.Remove(question);
        }
        /// <summary>
        /// 答问卷
        /// </summary>
        /// <param name="question"></param>
        /// <param name="answer"></param>
        /// <param name="club"></param>
        /// <param name="group"></param>
        /// <param name="uid"></param>
        /// <param name="wxid"></param>
        /// <returns></returns>
        public (bool result, string? desc) SetAnswer(string question, string answer, Club club, Group group, string wxid, string uid = "")
        {
            lock (_answerLocker)
            {
                try
                {
                    if (_questionnaires[question] is null) return (false, "调查问题不存在");
                    _questionnaires[question]["answer"] = _questionnaires[question]["answer"] ?? new JObject();
                    _questionnaires[question]["answer"][wxid] = new JObject();
                    _questionnaires[question]["answer"][wxid]["time"] = TimeHelper.ToTimeStamp();
                    _questionnaires[question]["answer"][wxid]["club_rid"] = club.RID;
                    _questionnaires[question]["answer"][wxid]["club_name"] = club.Name;
                    _questionnaires[question]["answer"][wxid]["club_channel"] = include.club.ChannelTypeDesc(club.ChannelType);
                    _questionnaires[question]["answer"][wxid]["club_dsit"] = club.DistDesc;
                    _questionnaires[question]["answer"][wxid]["group_wxid"] = group.WXID;
                    _questionnaires[question]["answer"][wxid]["group_name"] = group.Name;
                    _questionnaires[question]["answer"][wxid]["group_member_nick"] = group.Members.ContainsKey(wxid) ? group.Members[wxid].NickName : "";
                    _questionnaires[question]["answer"][wxid]["game_uid"] = uid;
                    _questionnaires[question]["answer"][wxid]["answer"] = answer;
                    var clubMember = _context.ClubsM.FindMember(uid);
                    if (clubMember is not null) _questionnaires[question][wxid]["game_name"] = clubMember.NameOrUID();
                    return (true, null);
                }
                catch (Exception ex)
                {
                    Context.Logger.WriteException(ex, $"QuestionnaireM.AnswerQuestionnaire");
                    return (false, "发生了未知错误");
                }
            }
        }

        public string QueryResponse(string question)
        {
            var response = JSONHelper.ParseString(_questionnaires[question]?["response"]);
            if (string.IsNullOrEmpty(response)) return "您的回复已被记录，谢谢支持！";
            else return response;
        }
        /// <summary>
        /// 生成问卷调查excel
        /// </summary>
        /// <param name="question"></param>
        /// <returns></returns>
        public string? GetQuestionnaireExcel(string question = "")
        {
            var fileName = $"OUT\\{ExcelHelper.GetFileName($"{include.files.File_Questionnaires}_{(string.IsNullOrEmpty(question) ? "所有" : question)}")}";
            var now = TimeHelper.ToTimeStamp();
            try
            {
                var content = new List<List<string>>
                    {
                        new List<string>
                        {
                            "问卷",
                            "回复",
                            "时间",
                            "机器人",
                            "微信群ID",
                            "微信群名",
                            "微信ID",
                            "微信名",
                            "俱乐部ID",
                            "俱乐部名",
                            "俱乐部渠道",
                            "俱乐部区组",
                            "游戏ID",
                            "游戏昵称",
                        },
                    };

                var keys = string.IsNullOrEmpty(question) ? JSONHelper.GetKeys(_questionnaires) : new List<string> { question };
                foreach (var key in keys)
                {
                    var data = _questionnaires[key];
                    if (data is null) continue;

                    data = data["answer"];
                    if (data is null) continue;

                    foreach (var item in data)
                    {
                        var wxid = item.Name;
                        content.Add(new List<string>
                                        {
                                            key,
                                            JSONHelper.ParseString(item.answer),
                                            TimeHelper.ChinsesTimeDesc(item.time),
                                            JSONHelper.ParseString(item.robot),
                                            JSONHelper.ParseString(item.group_wxid),
                                            JSONHelper.ParseString(item.group_name),
                                            wxid,
                                            JSONHelper.ParseString(item.group_member_nick),
                                            JSONHelper.ParseString(item.club_rid),
                                            JSONHelper.ParseString(item.club_name),
                                            JSONHelper.ParseString(item.club_channel),
                                            JSONHelper.ParseString(item.club_dsit),
                                            JSONHelper.ParseString(item.game_uid),
                                            JSONHelper.ParseString(item.game_name),
                                        });
                    }
                }

                return ExcelHelper.SaveSingleWorksheet(content, fileName, "问卷调查");
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, $"QuestionnaireM.GetQuestionnaireExcel");
                return null;
            }
        }
        #endregion

    }
}
