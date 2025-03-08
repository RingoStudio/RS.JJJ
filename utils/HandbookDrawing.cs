using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Utilities;
using RS.Snail.QCSDK.misc;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using RS.Snail.JJJ.boot;

namespace RS.Snail.JJJ.utils
{
    internal class HandbookDrawing
    {
        private static string TAG = "HandbookDrawing";
        #region COLLECTION
        /// <summary>
        /// 贵重一图流
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Bitmap GetCollectionHandbookThum(dynamic data)
        {
            int ratio = 2;
            if (data is null || JSONHelper.GetCount(data) == 0) return null;

            JJJ.Client.core.game.include.CollectionRank rank = (JJJ.Client.core.game.include.CollectionRank)JSONHelper.ParseInt(data["rank"]);
            JJJ.Client.core.game.include.CollectionType type = (JJJ.Client.core.game.include.CollectionType)JSONHelper.ParseInt(data["type"]);
            int rarity = JSONHelper.ParseInt(data["rarity"]);
            bool canSynchro = JSONHelper.ParseInt(data["can_synchro"]) > 0;

            Bitmap icon = GetResourcePNG("COLLECTION_HN_MAINBG_2");
            var b = new System.Drawing.Bitmap(icon.Width, icon.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var g = Graphics.FromImage(b);

            var iconSize = new Size(0, 0);
            var iconPos = new Point(0, 0);
            int authorY = 0;

            var commonBrush = new SolidBrush(System.Drawing.Color.FromArgb(80, 80, 80));
            var borderBrush = new SolidBrush(Color.FromArgb(80, 80, 80));
            SolidBrush colorBrush = rank switch
            {
                JJJ.Client.core.game.include.CollectionRank.Blue => new SolidBrush(System.Drawing.Color.FromArgb(0, 131, 183)),
                JJJ.Client.core.game.include.CollectionRank.Purple => new SolidBrush(System.Drawing.Color.FromArgb(147, 50, 178)),
                JJJ.Client.core.game.include.CollectionRank.Orange => new SolidBrush(System.Drawing.Color.FromArgb(181, 90, 32)),
                _ => new SolidBrush(System.Drawing.Color.FromArgb(36, 120, 26)),
            };

            #region BACK GROUND
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(Color.Transparent);

            g.DrawImage(icon, new Rectangle(0, 0, icon.Width, icon.Height), new RectangleF(0, 0, icon.Width, icon.Height), GraphicsUnit.Pixel);
            #endregion

            #region COLLECTION ICON BACK GROUND
            icon = rank switch
            {
                JJJ.Client.core.game.include.CollectionRank.Blue => GetResourcePNG("COLLECTION_HN_ICONBG_B"),
                JJJ.Client.core.game.include.CollectionRank.Purple => GetResourcePNG("COLLECTION_HN_ICONBG_P"),
                JJJ.Client.core.game.include.CollectionRank.Orange => GetResourcePNG("COLLECTION_HN_ICONBG_O"),
                _ => GetResourcePNG("COLLECTION_HN_ICONBG_G"),
            };
            g.DrawImage(icon, new Rectangle(CenterPos2EdgePos(125 * ratio, icon.Width * ratio), CenterPos2EdgePos(120 * ratio, icon.Height * ratio), icon.Width * ratio, icon.Height * ratio), new RectangleF(0, 0, icon.Width, icon.Height), GraphicsUnit.Pixel);
            #endregion

            #region COLLECTION ICON
            icon = LoadFile(JSONHelper.ParseString(data["icon"]));
            if (icon is not null) g.DrawImage(icon, new Rectangle(CenterPos2EdgePos(125 * ratio, icon.Width * ratio), CenterPos2EdgePos(120 * ratio, icon.Height * ratio), icon.Width * ratio, icon.Height * ratio), new RectangleF(0, 0, icon.Width, icon.Height), GraphicsUnit.Pixel);
            #endregion

            #region WATER MARK
            icon = GetResourcePNG("COLLECTION_HN_ICONCO");
            g.DrawImage(icon, new Rectangle(CenterPos2EdgePos(125 * ratio, icon.Width * ratio), CenterPos2EdgePos(120 * ratio, icon.Height * ratio), icon.Width * ratio, icon.Height * ratio), new RectangleF(0, 0, icon.Width, icon.Height), GraphicsUnit.Pixel);
            #endregion

            #region TYPE ICON
            icon = type switch
            {
                JJJ.Client.core.game.include.CollectionType.Art => LoadFile(@"RES\IMG\ui\collection\art.png"),
                JJJ.Client.core.game.include.CollectionType.Culture => LoadFile(@"RES\IMG\ui\collection\culture.png"),
                JJJ.Client.core.game.include.CollectionType.Influence => LoadFile(@"RES\IMG\ui\collection\influence.png"),
                JJJ.Client.core.game.include.CollectionType.Technology => LoadFile(@"RES\IMG\ui\collection\technology.png"),
                _ => LoadFile(@"RES\IMG\ui\collection\religion.png"),
            };
            iconSize = icon.Size;
            ScaleWHByOneLimit(ref iconSize, 45 * ratio);
            g.DrawImage(icon, new Rectangle(CenterPos2EdgePos(275 * ratio, iconSize.Width), CenterPos2EdgePos(45 * ratio, iconSize.Height), iconSize.Width, iconSize.Height), new RectangleF(0, 0, icon.Width, icon.Height), GraphicsUnit.Pixel);
            #endregion

            #region NAME
            iconPos.X = 300 * ratio;
            iconPos.Y = 32 * ratio;
            DrawString(g, iconPos, JSONHelper.ParseString(data["name"]), FontHelper.FontWeight.Black, 30d * ratio, colorBrush, StringAlignment.LEFT);
            #endregion

            #region RARITY
            if (rank == JJJ.Client.core.game.include.CollectionRank.Orange)
            {
                if (rarity == 1) icon = LoadFile(@"RES\IMG\ui\collection\rarity_5_1.png");
                else if (rarity == 2) icon = LoadFile(@"RES\IMG\ui\collection\rarity_5_2.png");
                else if (rarity == 3) icon = LoadFile(@"RES\IMG\ui\collection\rarity_5_3.png");
                else goto HO_AFTER_RARITY;
            }
            else if (rank == JJJ.Client.core.game.include.CollectionRank.Purple)
            {
                if (rarity == 1) icon = LoadFile(@"RES\IMG\ui\collection\rarity_4_1.png");
                else if (rarity == 2) icon = LoadFile(@"RES\IMG\ui\collection\rarity_4_2.png");
                else if (rarity == 3) icon = LoadFile(@"RES\IMG\ui\collection\rarity_4_3.png");
                else goto HO_AFTER_RARITY;
            }
            else goto HO_AFTER_RARITY;
            iconSize = icon.Size;
            ScaleWHByTwoLimit(ref iconSize, 0, 44 * ratio);
            g.DrawImage(icon, new Rectangle(900 * ratio - iconSize.Width / 2, CenterPos2EdgePos(45 * ratio, iconSize.Height), iconSize.Width, iconSize.Height), new RectangleF(0, 0, icon.Width, icon.Height), GraphicsUnit.Pixel);
#endregion

HO_AFTER_RARITY:

#region DATE
            string date = JSONHelper.ParseString(data["date"]);
            if (!string.IsNullOrEmpty(date))
            {
                iconPos.X = 950 * ratio;
                iconPos.Y = 95 * ratio;
                DrawString(g, iconPos, date, FontHelper.FontWeight.Black, 23 * ratio, commonBrush, StringAlignment.RIGHT);
            }
            #endregion

            #region GET WAY
            string getway = JSONHelper.ParseString(data["getway"]);
            if (!string.IsNullOrEmpty(getway))
            {
                iconPos.X = 950 * ratio;
                iconPos.Y = string.IsNullOrEmpty(date) ? 90 * ratio : 140 * ratio;
                DrawString(g, iconPos, "获取途径", FontHelper.FontWeight.Black, 12 * ratio, commonBrush, StringAlignment.RIGHT);

                iconPos.Y += 18 * ratio;
                foreach (var line in getway.Split("\n"))
                {
                    DrawString(g, iconPos, line, FontHelper.FontWeight.Medium, 12 * ratio, commonBrush, StringAlignment.RIGHT);
                    iconPos.Y += 18 * ratio;
                }
            }
            #endregion

            #region FIVE
            var attribList = robot.include.club.FiveAttribs2;
            iconPos.X = 30 * ratio;
            iconPos.Y = 210 * ratio;
            foreach (var attrib in attribList)
            {
                var isLight = JJJ.Client.core.game.include.collection.CollectionTypeAttrib(type) == attrib;

                iconPos.Y += 30 * ratio;
                DrawString(g, iconPos, $"{robot.include.club.FiveAttribDesc(attrib)} {JSONHelper.ParseString(data[attrib])}", FontHelper.FontWeight.Black, 12 * ratio, isLight ? colorBrush : commonBrush, StringAlignment.LEFT);
            }

            authorY = iconPos.Y;
            #endregion

            #region LIGHT PROPS
            var propTitle = rank switch
            {
                JJJ.Client.core.game.include.CollectionRank.Blue => "光环技能 (3星/4星)",
                JJJ.Client.core.game.include.CollectionRank.Purple => "光环技能 (3星/4星/5星)",
                JJJ.Client.core.game.include.CollectionRank.Orange => "光环技能 (3星/4星/5星/6星)",
                _ => "光环技能 (3星)",
            };
            if (rank == JJJ.Client.core.game.include.CollectionRank.Green)
            {
                iconPos.X = 250 * ratio;
                iconPos.Y = 100 * ratio;
                DrawString(g, iconPos, propTitle, FontHelper.FontWeight.Black, 16 * ratio, commonBrush, StringAlignment.LEFT);

                propTitle = JSONHelper.ParseString(data["light"]);
                foreach (var line in propTitle.Split("\n"))
                {
                    iconPos.Y += 25 * ratio;
                    DrawString(g, iconPos, line, FontHelper.FontWeight.Medium, 16 * ratio, commonBrush, StringAlignment.LEFT);
                }

                goto HO_AFTER_PROPS;
            }
            else
            {
                iconPos.X = 250 * ratio;
                iconPos.Y = 80 * ratio;
                DrawString(g, iconPos, propTitle, FontHelper.FontWeight.Black, 12 * ratio, commonBrush, StringAlignment.LEFT);

                propTitle = JSONHelper.ParseString(data["light"]);
                foreach (var line in propTitle.Split("\n"))
                {
                    iconPos.Y += 19 * ratio;
                    DrawString(g, iconPos, line, FontHelper.FontWeight.Medium, 12 * ratio, commonBrush, StringAlignment.LEFT);
                }

                propTitle = JSONHelper.ParseString(data["awake"]);
                iconPos.Y += 19 * ratio;
                DrawString(g, iconPos, propTitle, FontHelper.FontWeight.Medium, 12 * ratio, colorBrush, StringAlignment.LEFT);
                if (canSynchro)
                {
                    propTitle = JSONHelper.ParseString(data["synchro_prop"]);
                    if (!string.IsNullOrEmpty(propTitle))
                    {
                        iconPos.Y += 19 * ratio;
                        DrawString(g, iconPos, propTitle, FontHelper.FontWeight.Medium, 12 * ratio, colorBrush, StringAlignment.LEFT);
                    }
                }
            }
            #endregion

            #region ENCHASE
            string enchase = JSONHelper.ParseString(data["enchase"]);
            propTitle = rank switch
            {
                JJJ.Client.core.game.include.CollectionRank.Blue => "镶嵌技能 (初级改造)",
                JJJ.Client.core.game.include.CollectionRank.Purple => "镶嵌技能 (初级改造/中级改造)",
                JJJ.Client.core.game.include.CollectionRank.Orange => "镶嵌技能 (初级改造/中级改造/高级改造)",
                _ => "",
            };
            if (!string.IsNullOrEmpty(enchase) && !string.IsNullOrEmpty(propTitle))
            {
                if (canSynchro) iconPos.Y += 25 * ratio;
                else iconPos.Y += 30 * ratio;
                DrawString(g, iconPos, propTitle, FontHelper.FontWeight.Black, 12 * ratio, commonBrush, StringAlignment.LEFT);

                foreach (var line in enchase.Split("\n"))
                {
                    iconPos.Y += 19 * ratio;
                    DrawString(g, iconPos, line.Replace(StringHelper.FixContent("赋予效果:"), ""), FontHelper.FontWeight.Medium, 12 * ratio, commonBrush, StringAlignment.LEFT);
                }
            }
            #endregion

            #region RETONATE
            if (data["resonate"] is not null && JSONHelper.GetCount(data["resonate"]) > 0)
            {
                if (canSynchro) iconPos.Y += 25 * ratio;
                else iconPos.Y += 30 * ratio;
                foreach (var item in data["resonate"])
                {
                    var info = item.Value;
                    var names = new List<string>();
                    foreach (var subItem in info["collections"])
                    {
                        names.Add(JSONHelper.ParseString(subItem.Value["name"]));
                    }

                    DrawString(g, iconPos, string.Join("+", names), FontHelper.FontWeight.Black, 12 * ratio, colorBrush, StringAlignment.LEFT);
                    iconPos.Y += 19 * ratio;

                    DrawString(g, iconPos, JSONHelper.ParseString(info["prop"]).Replace("\n", "，"), FontHelper.FontWeight.Medium, 12 * ratio, colorBrush, StringAlignment.LEFT);
                    iconPos.Y += 19 * ratio;
                }
            }
#endregion

HO_AFTER_PROPS:

#region AUTHOR
            iconPos.X = 950 * ratio;
            iconPos.Y = authorY - 4 * ratio;
            propTitle = $"Cr. 最强搜索叽 2023";
            DrawString(g, iconPos, propTitle, FontHelper.FontWeight.Bold, 8 * ratio, commonBrush, StringAlignment.RIGHT);
            propTitle = TimeHelper.ChinsesTimeDesc(TimeHelper.ToTimeStamp()).Split(" ").First();
            iconPos.Y += 12 * ratio;
            DrawString(g, iconPos, propTitle, FontHelper.FontWeight.Medium, 8 * ratio, commonBrush, StringAlignment.RIGHT);
            #endregion
            return b;
        }
        #endregion
        #region MONSTER
        ///// <summary>
        ///// 怪物卡片
        ///// </summary>
        ///// <param name="id"></param>
        ///// <param name="extra"></param>
        ///// <returns></returns>
        //public static Bitmap GetMonsterHandbookThum(string id, dynamic extra = null)
        //{
        //    extra = extra ?? new JObject();
        //    var data = configs.Core.Instence().Context.MonsterM.QueryMonsterDetailInfo(id);
        //    if (data is null) return null;

        //    bool isSonCombat = JSONHelper.ParseBool(data.is_son_combat);
        //    JJJ.Client.core.game.include.MonsterType monsterType = (JJJ.Client.core.game.include.MonsterType)JSONHelper.ParseInt(data.type);
        //    int ratio = 2;

        //    Bitmap icon = GetResourcePNG("COLLECTION_HN_MAINBG_2");
        //    var b = new System.Drawing.Bitmap(icon.Width, icon.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        //    var g = Graphics.FromImage(b);

        //    var iconSize = new Size(0, 0);
        //    var iconPos = new Point(0, 0);
        //    int authorY = 0;

        //    var commonBrush = new SolidBrush(System.Drawing.Color.FromArgb(80, 80, 80));
        //    var blueBrush = new SolidBrush(Color.FromArgb(68, 114, 196));
        //    SolidBrush colorBrush = (Core.game.include.CollectionRank)JSONHelper.ParseInt(data.rank, 0) switch
        //    {
        //        JJJ.Client.core.game.include.CollectionRank.Blue => new SolidBrush(System.Drawing.Color.FromArgb(0, 131, 183)),
        //        JJJ.Client.core.game.include.CollectionRank.Purple => new SolidBrush(System.Drawing.Color.FromArgb(147, 50, 178)),
        //        JJJ.Client.core.game.include.CollectionRank.Orange => new SolidBrush(System.Drawing.Color.FromArgb(181, 90, 32)),
        //        _ => commonBrush,
        //    };

        //    #region BACKGROUND
        //    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        //    g.Clear(Color.Transparent);

        //    g.DrawImage(icon, new Rectangle(0, 0, icon.Width, icon.Height), new RectangleF(0, 0, icon.Width, icon.Height), GraphicsUnit.Pixel);

        //    icon = GetResourcePNG("COLLECTION_HN_ICONBG_R");
        //    g.DrawImage(icon, new Rectangle(CenterPos2EdgePos(125 * ratio, icon.Width * ratio), CenterPos2EdgePos(120 * ratio, icon.Height * ratio), icon.Width * ratio, icon.Height * ratio), new RectangleF(0, 0, icon.Width, icon.Height), GraphicsUnit.Pixel);
        //    #endregion

        //    #region MONSTER ICON
        //    var path = JSONHelper.ParseString(data["icon"]);

        //    icon = LoadFile(path);
        //    if (icon is not null)
        //    {
        //        iconSize = icon.Size;
        //        ScaleWHByOneLimit(ref iconSize, 240 * ratio);
        //        g.DrawImage(icon, new Rectangle(CenterPos2EdgePos(125 * ratio, iconSize.Width), CenterPos2EdgePos(120 * ratio, iconSize.Height), iconSize.Width, iconSize.Height), new RectangleF(0, 0, icon.Width, icon.Height), GraphicsUnit.Pixel);
        //    }
        //    #endregion

        //    #region INK
        //    icon = GetResourcePNG("COLLECTION_HN_ICONCO");
        //    g.DrawImage(icon, new Rectangle(CenterPos2EdgePos(125 * ratio, icon.Width * ratio), CenterPos2EdgePos(120 * ratio, icon.Height * ratio), icon.Width * ratio, icon.Height * ratio), new RectangleF(0, 0, icon.Width, icon.Height), GraphicsUnit.Pixel);
        //    #endregion

        //    #region NAME
        //    int nameOffset = 0;
        //    var fontSize = 30 * ratio;
        //    string name = configs.Core.Instence().Context.LocM.GetDesc2(data["name"]);
        //    if (name.Length > 8) fontSize = 27 * ratio;
        //    iconPos.X = 250 * ratio;
        //    iconPos.Y = 32 * ratio;
        //    nameOffset = DrawString(g, iconPos, name, FontHelper.FontWeight.Bold, fontSize, colorBrush, StringAlignment.LEFT);
        //    #endregion

        //    #region WEAKNESS
        //    dynamic weakness = data.weakness;
        //    if (!isSonCombat && weakness is JObject && JSONHelper.GetCount(weakness) > 0)
        //    {
        //        int iconLimit = 0;
        //        if (/*monsterType ==JJJ.Client.core.game.include.MonsterType.AREA_BOSS ||*/ monsterType == JJJ.Client.core.game.include.MonsterType.SON_TOWER_SPECIAL)
        //        {
        //            // 弱点放在左
        //            iconLimit = 50 * ratio;
        //            iconPos.Y = 85 * ratio;
        //            iconPos.X = 250 * ratio;

        //            var weaknessDesc = JSONHelper.ParseString(weakness.desc_2);
        //            icon = LoadFile(JSONHelper.ParseString(weakness.icon));
        //            iconSize = icon.Size;
        //            ScaleWHByOneLimit(ref iconSize, iconLimit);
        //            g.DrawImage(icon, new Rectangle(iconPos.X, iconPos.Y - 20 * ratio, iconSize.Width, iconSize.Height), new RectangleF(0, 0, icon.Width, icon.Height), GraphicsUnit.Pixel);

        //            iconPos.X += 45 * ratio;
        //            DrawString(g, iconPos, weaknessDesc, FontHelper.FontWeight.Black, 15 * ratio, colorBrush, StringAlignment.LEFT);
        //        }
        //        else
        //        {
        //            // 弱点放右边
        //            iconLimit = 50 * ratio;
        //            iconPos.Y = 40 * ratio;
        //            iconPos.X = Math.Max(nameOffset + 10 * ratio, 548 * ratio);

        //            var weaknessDesc = JSONHelper.ParseString(weakness.desc_2);
        //            icon = LoadFile(JSONHelper.ParseString(weakness.icon));
        //            iconSize = icon.Size;
        //            ScaleWHByOneLimit(ref iconSize, iconLimit);
        //            g.DrawImage(icon, new Rectangle(iconPos.X, iconPos.Y - 20 * ratio, iconSize.Width, iconSize.Height), new RectangleF(0, 0, icon.Width, icon.Height), GraphicsUnit.Pixel);

        //            iconPos.X += 45 * ratio;
        //            DrawString(g, iconPos, weaknessDesc, FontHelper.FontWeight.Black, 15 * ratio, colorBrush, StringAlignment.LEFT);
        //        }


        //    }
        //    #endregion

        //    #region BASE ATTRIB
        //    //  if (/*monsterType ==JJJ.Client.core.game.include.MonsterType.AREA_BOSS ||*/ monsterType ==JJJ.Client.core.game.include.MonsterType.SON_TOWER_SPECIAL) { }
        //    iconPos.Y = 50 * ratio;
        //    long attribVal = 0;
        //    foreach (var attrib in new string[] { "max_hp", "attack", "defense", "combo" })
        //    {
        //        iconPos.Y += 35 * ratio;
        //        iconPos.X = 250 * ratio;
        //        var iconKey = "";
        //        if (isSonCombat) iconKey = $"attrib_small\\son_{attrib}.png";
        //        else iconKey = $"attrib_small\\{(attrib == "attack" ? "attack1" : attrib)}.png";
        //        icon = LoadFile(iconKey);
        //        if (extra[attrib] is not null) attribVal = JSONHelper.ParseLong(extra[attrib]);
        //        else attribVal = JSONHelper.ParseLong(data[attrib]);
        //        iconSize = icon.Size;
        //        ScaleWHByOneLimit(ref iconSize, 40 * ratio);
        //        g.DrawImage(icon, new Rectangle(iconPos.X + 5 * ratio, iconPos.Y - 16 * ratio, iconSize.Width, iconSize.Height), new RectangleF(0, 0, icon.Width, icon.Height), GraphicsUnit.Pixel);

        //        iconPos.X = (250 + 45) * ratio;
        //        DrawString(g, iconPos, attribVal.ToString("N0"), FontHelper.FontWeight.Black, 15 * ratio, colorBrush, StringAlignment.LEFT);
        //    }
        //    #endregion

        //    #region RECORD INFO 
        //    List<string> descList = JSONHelper.ParseStringList(data.extra_desc);
        //    descList.Insert(0, JSONHelper.ParseString(data.type_desc));
        //    if (descList.Count > 0)
        //    {
        //        iconPos.X = 125 * ratio;
        //        iconPos.Y = (315 - descList.Count * 13) * ratio;
        //        for (int i = 0; i < descList.Count; i++)
        //        {
        //            DrawString(g, iconPos, descList[i], i > 0 ? FontHelper.FontWeight.Black : FontHelper.FontWeight.Black,
        //               i > 0 ? 13 * ratio : 15 * ratio, commonBrush, StringAlignment.CENTER);
        //            iconPos.Y += 26 * ratio;
        //        }
        //    }
        //    #endregion

        //    #region BONUS

        //    if (monsterType == JJJ.Client.core.game.include.MonsterType.SON_TOWER_SPECIAL && data.prop is JArray && JSONHelper.GetCount(data.prop) > 0)
        //    {
        //        // 战术记录 额外显示各级加成
        //        var bonus = data.prop;
        //        iconPos.Y = 52 * ratio;
        //        foreach (var prop in bonus)
        //        {
        //            iconPos.X = 550 * ratio;
        //            iconPos.Y += 35 * ratio;
        //            icon = LoadFile(JSONHelper.ParseString(prop.icon));

        //            if (icon is not null)
        //            {
        //                iconSize = icon.Size;
        //                ScaleWHByOneLimit(ref iconSize, 35 * ratio);
        //                g.DrawImage(icon, new Rectangle(iconPos.X + 5 * ratio, iconPos.Y - 14 * ratio, iconSize.Width, iconSize.Height), new RectangleF(0, 0, icon.Width, icon.Height), GraphicsUnit.Pixel);
        //            }

        //            iconPos.X = (550 + 45) * ratio;
        //            DrawString(g, iconPos, JSONHelper.ParseString(prop.desc), FontHelper.FontWeight.Black, 14 * ratio, colorBrush, StringAlignment.LEFT);
        //        }
        //    }

        //    if (monsterType == JJJ.Client.core.game.include.MonsterType.AREA_BOSS)
        //    {
        //        dynamic bonus = data.bonus;
        //        if (bonus is JArray && JSONHelper.GetCount(bonus) > 0)
        //        {
        //            iconPos.Y = 52 * ratio;

        //            int xOffset = 550;

        //            foreach (var bonusInfo in bonus)
        //            {
        //                iconPos.Y += 35 * ratio;
        //                if (bonusInfo.title is not null)
        //                {
        //                    iconPos.X = xOffset * ratio;
        //                    DrawString(g, iconPos, JSONHelper.ParseString(bonusInfo.title), FontHelper.FontWeight.Bold, 16 * ratio, commonBrush, StringAlignment.LEFT);
        //                    iconPos.Y += 2 * ratio;
        //                }
        //                else
        //                {
        //                    var bonusDesc = JSONHelper.ParseString(bonusInfo.simple_desc);
        //                    icon = LoadFile(JSONHelper.ParseString(bonusInfo.icon));
        //                    iconPos.X = xOffset * ratio;
        //                    if (icon is not null)
        //                    {
        //                        iconSize = icon.Size;
        //                        ScaleWHByOneLimit(ref iconSize, 35 * ratio);
        //                        iconPos.X = xOffset * ratio;
        //                        g.DrawImage(icon, new Rectangle(iconPos.X + 5 * ratio, iconPos.Y - 12 * ratio, iconSize.Width, iconSize.Height), new RectangleF(0, 0, icon.Width, icon.Height), GraphicsUnit.Pixel);
        //                        iconPos.X = (xOffset + 45) * ratio;
        //                    }

        //                    DrawString(g, iconPos, bonusDesc, FontHelper.FontWeight.Black, 14 * ratio, commonBrush, StringAlignment.LEFT);
        //                }

        //            }
        //        }
        //    }
        //    else
        //    {
        //        dynamic bonus = data.bonus;
        //        if (bonus is JArray && JSONHelper.GetCount(bonus) > 0)
        //        {
        //            iconPos.X = 250 * ratio;
        //            iconPos.Y = (32 + 215) * ratio;
        //            DrawString(g, iconPos, langs.ResourceLang._model_monster_desc_handbook_title_win_bonus, FontHelper.FontWeight.Bold, 16 * ratio, commonBrush, StringAlignment.LEFT);
        //            iconPos.Y += 35 * ratio;
        //            int xOffset = 250;
        //            foreach (var bonusInfo in bonus)
        //            {
        //                var bonusDesc = JSONHelper.ParseString(bonusInfo.simple_desc);
        //                icon = LoadFile(JSONHelper.ParseString(bonusInfo.icon));
        //                iconPos.X = xOffset * ratio;

        //                if (icon is not null)
        //                {
        //                    iconSize = icon.Size;
        //                    ScaleWHByOneLimit(ref iconSize, 35 * ratio);

        //                    g.DrawImage(icon, new Rectangle(iconPos.X + 5 * ratio, iconPos.Y - 12 * ratio, iconSize.Width, iconSize.Height), new RectangleF(0, 0, icon.Width, icon.Height), GraphicsUnit.Pixel);
        //                    iconPos.X = (xOffset + 45) * ratio;
        //                }

        //                DrawString(g, iconPos, bonusDesc, FontHelper.FontWeight.Black, 14 * ratio, colorBrush, StringAlignment.LEFT);

        //                iconPos.Y += 35 * ratio;
        //                if (iconPos.Y > 352 * ratio)
        //                {
        //                    xOffset = 550;
        //                    iconPos.Y = 282 * ratio;
        //                }
        //            }
        //        }
        //    }
        //    #endregion

        //    #region PROPS

        //    List<string> props = JSONHelper.ParseStringList(data.prop);
        //    if (!isSonCombat && monsterType != JJJ.Client.core.game.include.MonsterType.SON_TOWER_SPECIAL && props.Count > 0)
        //    {

        //        icon = LoadFile("RES\\IMG\\ui\\tour_new\\tip.png");
        //        iconSize = icon.Size;
        //        ScaleWHByOneLimit(ref iconSize, 35 * ratio);
        //        if (monsterType == JJJ.Client.core.game.include.MonsterType.AREA_BOSS)
        //        {
        //            // 探索使徒的加成放在下面
        //            iconPos.Y = 225 * ratio;
        //            foreach (var prop in props)
        //            {
        //                iconPos.X = 250 * ratio;
        //                iconPos.Y += 35 * ratio;
        //                g.DrawImage(icon, new Rectangle(iconPos.X + 5 * ratio, iconPos.Y - 12 * ratio, iconSize.Width, iconSize.Height), new RectangleF(0, 0, icon.Width, icon.Height), GraphicsUnit.Pixel);

        //                iconPos.X = (250 + 45) * ratio;
        //                DrawString(g, iconPos, prop, FontHelper.FontWeight.Bold, 14 * ratio, blueBrush, StringAlignment.LEFT);
        //            }
        //        }
        //        else
        //        {
        //            iconPos.Y = 52 * ratio;
        //            foreach (var prop in props)
        //            {
        //                iconPos.X = 550 * ratio;
        //                iconPos.Y += 35 * ratio;
        //                g.DrawImage(icon, new Rectangle(iconPos.X + 5 * ratio, iconPos.Y - 12 * ratio, iconSize.Width, iconSize.Height), new RectangleF(0, 0, icon.Width, icon.Height), GraphicsUnit.Pixel);

        //                iconPos.X = (550 + 45) * ratio;
        //                DrawString(g, iconPos, prop, FontHelper.FontWeight.Bold, 14 * ratio, blueBrush, StringAlignment.LEFT);
        //            }
        //        }

        //    }
        //    #endregion

        //    #region AUTHOR
        //    iconPos.X = 950 * ratio;
        //    iconPos.Y = 350 * ratio;
        //    var title = $"Cr. {langs.ResourceLang._name_version}";
        //    DrawString(g, iconPos, title, FontHelper.FontWeight.Bold, 8 * ratio, commonBrush, StringAlignment.RIGHT);
        //    title = TimeHelper.ChinsesTimeDesc(TimeHelper.ToTimeStamp()).Split(" ").First();
        //    iconPos.Y += 12 * ratio;
        //    DrawString(g, iconPos, title, FontHelper.FontWeight.Medium, 8 * ratio, commonBrush, StringAlignment.RIGHT);
        //    #endregion

        //    return b;
        //}
        #endregion

        #region GROUP WAR
        ///// <summary>
        ///// 矿点表
        ///// </summary>
        ///// <returns></returns>
        //public static Bitmap GetGroupwarEventMinesThum()
        //{
        //    var data = configs.Core.Instence().Context.GroupWarDataM.QueryEventMgrMapList(JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_MINE, -1, true);
        //    var title = configs.Core.Instence().Context.GroupWarDataM.GetGroupEventThumTitleInfo();

        //    var icon = BitmapHelper.BitmapSourceToBitmap(BitmapHelper.LoadResource("gw/event_export_template_mine"));

        //    var b = new System.Drawing.Bitmap(icon.Width, icon.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        //    var g = Graphics.FromImage(b);

        //    var iconSize = new Size(0, 0);
        //    var iconPos = new Point(0, 0);

        //    var commonBrush = new SolidBrush(System.Drawing.Color.FromArgb(80, 80, 80));

        //    #region BACKGROUND
        //    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        //    g.Clear(Color.Transparent);

        //    g.DrawImage(icon, new Rectangle(0, 0, icon.Width, icon.Height), new RectangleF(0, 0, icon.Width, icon.Height), GraphicsUnit.Pixel);
        //    #endregion

        //    #region TITLE
        //    iconPos.X = 1920 / 2;
        //    iconPos.Y = 475;
        //    DrawString(g, iconPos, title, FontHelper.FontWeight.Bold, 30, commonBrush, StringAlignment.CENTER);
        //    #endregion

        //    #region MINES
        //    var areaCount = new Dictionary<int, int>();

        //    foreach (var item in data)
        //    {
        //        var area = JSONHelper.ParseInt(item.area);
        //        var row = JSONHelper.ParseInt(item.row);
        //        var col = JSONHelper.ParseInt(item.col);
        //        var name = GetGWGridShortName(JSONHelper.ParseString(item.name));

        //        if (!areaCount.ContainsKey(area)) areaCount.Add(area, 0);

        //        iconPos.X = 204 + areaCount[area] * 200;
        //        iconPos.Y = 604 + (area - 1) * 80;

        //        DrawString(g, iconPos, $"[{row:00}行{col:00}列]{name}", FontHelper.FontWeight.Black, 20, commonBrush, StringAlignment.CENTER);

        //        areaCount[area]++;
        //    }
        //    #endregion

        //    return b;
        //}

        ///// <summary>
        ///// 事件表
        ///// </summary>
        ///// <returns></returns>
        //public static Bitmap GetGroupwarEventMultisThum()
        //{
        //    var data = configs.Core.Instence().Context.GroupWarDataM.QueryEventMgrMapList(JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_MULTI, -1, true);
        //    var singles = configs.Core.Instence().Context.GroupWarDataM.QueryEventMgrMapList(JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_SINGLE, -1, true);

        //    var title = configs.Core.Instence().Context.GroupWarDataM.GetGroupEventThumTitleInfo();

        //    var icon = BitmapHelper.BitmapSourceToBitmap(BitmapHelper.LoadResource("gw/event_export_template_multi"));

        //    var b = new System.Drawing.Bitmap(icon.Width, icon.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        //    var g = Graphics.FromImage(b);

        //    var iconSize = new Size(0, 0);
        //    var iconPos = new Point(0, 0);

        //    var commonBrush = new SolidBrush(System.Drawing.Color.FromArgb(80, 80, 80));

        //    #region BACKGROUND
        //    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        //    g.Clear(Color.Transparent);

        //    g.DrawImage(icon, new Rectangle(0, 0, icon.Width, icon.Height), new RectangleF(0, 0, icon.Width, icon.Height), GraphicsUnit.Pixel);
        //    #endregion

        //    #region TITLE
        //    iconPos.X = 1920 / 2;
        //    iconPos.Y = 475;
        //    DrawString(g, iconPos, title, FontHelper.FontWeight.Bold, 30, commonBrush, StringAlignment.CENTER);
        //    #endregion

        //    #region MULTIS
        //    var areaCount = new Dictionary<int, int>();

        //    foreach (var item in data)
        //    {
        //        var area = JSONHelper.ParseInt(item.area);
        //        var row = JSONHelper.ParseInt(item.row);
        //        var col = JSONHelper.ParseInt(item.col);
        //        var name = JSONHelper.ParseString(item.name);

        //        if (!areaCount.ContainsKey(area)) areaCount.Add(area, 0);

        //        iconPos.X = 410 + areaCount[area] * 600;
        //        iconPos.Y = 598 + (area - 1) * 80;

        //        DrawString(g, iconPos, $"[{row:00}行{col:00}列] {name}", FontHelper.FontWeight.Black, 30, commonBrush, StringAlignment.CENTER);

        //        areaCount[area]++;
        //    }
        //    #endregion

        //    #region SINGLES
        //    var names = new List<string>();
        //    foreach (var item in singles)
        //    {
        //        var area = JSONHelper.ParseInt(item.area);
        //        var row = JSONHelper.ParseInt(item.row);
        //        var col = JSONHelper.ParseInt(item.col);
        //        var name = JSONHelper.ParseString(item.name);

        //        names.Add($"[{row:00}行{col:00}列] {name}");
        //    }

        //    if (names.Count > 0)
        //    {
        //        iconPos.X = 960;
        //        iconPos.Y = 2595;
        //        DrawString(g, iconPos, string.Join(" | ", names), FontHelper.FontWeight.Black, 30, commonBrush, StringAlignment.CENTER);
        //    }
        //    #endregion

        //    return b;
        //}

        ///// <summary>
        ///// BOSS表
        ///// </summary>
        ///// <returns></returns>
        //public static Bitmap GetGroupwarEvenBossThum()
        //{
        //    var boss = configs.Core.Instence().Context.GroupWarDataM.QueryEventMgrMapList(JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_BOSS, -1, true);
        //    var monster = configs.Core.Instence().Context.GroupWarDataM.QueryEventMgrMapList(JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_MONSTER, -1, true);

        //    var title = configs.Core.Instence().Context.GroupWarDataM.GetGroupEventThumTitleInfo();

        //    var icon = BitmapHelper.BitmapSourceToBitmap(BitmapHelper.LoadResource("gw/event_export_template_boss"));

        //    var b = new System.Drawing.Bitmap(icon.Width, icon.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        //    var g = Graphics.FromImage(b);

        //    var iconSize = new Size(0, 0);
        //    var iconPos = new Point(0, 0);

        //    var commonBrush = new SolidBrush(System.Drawing.Color.FromArgb(80, 80, 80));

        //    #region BACKGROUND
        //    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        //    g.Clear(Color.Transparent);

        //    g.DrawImage(icon, new Rectangle(0, 0, icon.Width, icon.Height), new RectangleF(0, 0, icon.Width, icon.Height), GraphicsUnit.Pixel);
        //    #endregion

        //    #region TITLE
        //    iconPos.X = 1920 / 2;
        //    iconPos.Y = 475;
        //    DrawString(g, iconPos, title, FontHelper.FontWeight.Bold, 30, commonBrush, StringAlignment.CENTER);
        //    #endregion

        //    #region MONSTER
        //    var areaCount = new Dictionary<int, int>();

        //    foreach (var item in monster)
        //    {
        //        var area = JSONHelper.ParseInt(item.area);
        //        var row = JSONHelper.ParseInt(item.row);
        //        var col = JSONHelper.ParseInt(item.col);
        //        var name = GetGWGridShortName(JSONHelper.ParseString(item.name));

        //        if (!areaCount.ContainsKey(area)) areaCount.Add(area, 0);

        //        iconPos.X = 300 + areaCount[area] * 400;
        //        iconPos.Y = 600 + (area - 1) * 80;

        //        DrawString(g, iconPos, $"[{row:00}行{col:00}列] {name}", FontHelper.FontWeight.Black, 30, commonBrush, StringAlignment.CENTER);

        //        areaCount[area]++;
        //    }
        //    #endregion

        //    #region BOSS
        //    var names = new List<string>();
        //    foreach (var item in boss)
        //    {
        //        var area = JSONHelper.ParseInt(item.area);
        //        var row = JSONHelper.ParseInt(item.row);
        //        var col = JSONHelper.ParseInt(item.col);
        //        var name = JSONHelper.ParseString(item.name);

        //        iconPos.X = 1610;
        //        iconPos.Y = 600 + (area - 1) * 80;

        //        DrawString(g, iconPos, $"[{row:00}行{col:00}列] {name}", FontHelper.FontWeight.Black, 30, commonBrush, StringAlignment.CENTER);
        //    }
        //    #endregion

        //    return b;
        //}

        ///// <summary>
        ///// BOSS表
        ///// </summary>
        ///// <returns></returns>
        //public static Bitmap GetGroupwarEvenSumThum()
        //{
        //    var boss = configs.Core.Instence().Context.GroupWarDataM.QueryEventMgrMapList(JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_BOSS, -1, true);
        //    var monster = configs.Core.Instence().Context.GroupWarDataM.QueryEventMgrMapList(JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_MONSTER, -1, true);
        //    var mines = configs.Core.Instence().Context.GroupWarDataM.QueryEventMgrMapList(JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_MINE, -1, true);
        //    var multis = configs.Core.Instence().Context.GroupWarDataM.QueryEventMgrMapList(JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_MULTI, -1, true);
        //    var singles = configs.Core.Instence().Context.GroupWarDataM.QueryEventMgrMapList(JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_SINGLE, -1, true);

        //    var title = configs.Core.Instence().Context.GroupWarDataM.GetGroupEventThumTitleInfo();

        //    var icon = BitmapHelper.BitmapSourceToBitmap(BitmapHelper.LoadResource("gw/event_export_template_sum"));

        //    var b = new System.Drawing.Bitmap(icon.Width, icon.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        //    var g = Graphics.FromImage(b);

        //    var iconSize = new Size(0, 0);
        //    var iconPos = new Point(0, 0);

        //    var commonBrush = new SolidBrush(System.Drawing.Color.FromArgb(80, 80, 80));

        //    #region BACKGROUND
        //    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        //    g.Clear(Color.Transparent);

        //    g.DrawImage(icon, new Rectangle(0, 0, icon.Width, icon.Height), new RectangleF(0, 0, icon.Width, icon.Height), GraphicsUnit.Pixel);
        //    #endregion

        //    #region TITLE
        //    iconPos.X = 1920 / 2;
        //    iconPos.Y = 475;
        //    DrawString(g, iconPos, title, FontHelper.FontWeight.Bold, 30, commonBrush, StringAlignment.CENTER);
        //    #endregion

        //    #region MULTIS
        //    var areaDescs = new Dictionary<int, List<string>>();
        //    foreach (var item in multis)
        //    {
        //        var area = JSONHelper.ParseInt(item.area);
        //        var row = JSONHelper.ParseInt(item.row);
        //        var col = JSONHelper.ParseInt(item.col);
        //        if (!areaDescs.ContainsKey(area)) areaDescs.Add(area, new List<string>());
        //        areaDescs[area].Add($"{row:00}.{col:00}");
        //    }
        //    foreach (var item in areaDescs)
        //    {
        //        iconPos.X = 395;
        //        iconPos.Y = 596 + (item.Key - 1) * 80;
        //        DrawString(g, iconPos, string.Join(" | ", item.Value), FontHelper.FontWeight.Black, 30, commonBrush, StringAlignment.CENTER);
        //    }
        //    #endregion

        //    #region SINGLES
        //    var names = new List<string>();
        //    foreach (var item in singles)
        //    {
        //        var area = JSONHelper.ParseInt(item.area);
        //        var row = JSONHelper.ParseInt(item.row);
        //        var col = JSONHelper.ParseInt(item.col);
        //        var name = JSONHelper.ParseString(item.name);

        //        names.Add($"[{row:00}行{col:00}列] {name}");
        //    }

        //    if (names.Count > 0)
        //    {
        //        iconPos.X = 960;
        //        iconPos.Y = 2595;
        //        DrawString(g, iconPos, string.Join(" | ", names), FontHelper.FontWeight.Black, 30, commonBrush, StringAlignment.CENTER);
        //    }
        //    #endregion

        //    #region MINES
        //    areaDescs.Clear();
        //    foreach (var item in mines)
        //    {
        //        var area = JSONHelper.ParseInt(item.area);
        //        if (area < 15 || area > 20) continue;
        //        var row = JSONHelper.ParseInt(item.row);
        //        var col = JSONHelper.ParseInt(item.col);

        //        if (!areaDescs.ContainsKey(area)) areaDescs.Add(area, new List<string>());
        //        areaDescs[area].Add($"{row:00}.{col:00}");
        //    }
        //    foreach (var item in areaDescs)
        //    {
        //        iconPos.X = 1320;
        //        iconPos.Y = 80 + 598 + (item.Key - 15) * 80 * 2;
        //        DrawString(g, iconPos, string.Join(" | ", item.Value), FontHelper.FontWeight.Black, 25, commonBrush, StringAlignment.CENTER);
        //    }
        //    #endregion

        //    #region MONSTER
        //    areaDescs.Clear();
        //    foreach (var item in monster)
        //    {
        //        var area = JSONHelper.ParseInt(item.area);
        //        if (area < 15) continue;
        //        int row = JSONHelper.ParseInt(item.row);
        //        int col = JSONHelper.ParseInt(item.col);
        //        string name = GetGWGridShortName(JSONHelper.ParseString(item.name));

        //        if (!areaDescs.ContainsKey(area)) areaDescs.Add(area, new List<string>());
        //        areaDescs[area].Add($"{name} {row:00}.{col:00}");
        //    }
        //    foreach (var item in areaDescs)
        //    {
        //        iconPos.X = 1340;
        //        iconPos.Y = 80 + 600 + (item.Key - 3) * 80;
        //        DrawString(g, iconPos, string.Join(" | ", item.Value), FontHelper.FontWeight.Black, 30, commonBrush, StringAlignment.CENTER);
        //    }
        //    #endregion

        //    #region BOSS
        //    names.Clear();
        //    foreach (var item in boss)
        //    {
        //        var area = JSONHelper.ParseInt(item.area);
        //        if (area < 15) continue;
        //        var row = JSONHelper.ParseInt(item.row);
        //        var col = JSONHelper.ParseInt(item.col);
        //        var pos = "中";
        //        if (col < 4) pos = "左";
        //        else if (col > 4) pos = "右";
        //        names.Add($"{area}{pos}{col}");
        //    }
        //    if (names.Count > 0)
        //    {
        //        iconPos.X = 1320;
        //        iconPos.Y = 2440;
        //        DrawString(g, iconPos, string.Join(" | ", names), FontHelper.FontWeight.Black, 30, commonBrush, StringAlignment.CENTER);
        //    }
        //    #endregion

        //    return b;
        //}

        //public static Bitmap GetGroupwarEvenMaphum(int area)
        //{
        //    #region FILTER ROWS
        //    var rows = new List<int>();
        //    var focusPoses = new List<(int row, int col)>();

        //    var includeEventTypes = new List<int>();
        //    if (configs.Settings.Instence().GWEventMgrMarkTypeMine) includeEventTypes.Add(JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_MINE);
        //    if (configs.Settings.Instence().GWEventMgrMarkTypeMonster) includeEventTypes.Add(JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_MONSTER);
        //    if (configs.Settings.Instence().GWEventMgrMarkTypeSingle) includeEventTypes.Add(JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_SINGLE);
        //    if (configs.Settings.Instence().GWEventMgrMarkTypeMulti) includeEventTypes.Add(JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_MULTI);
        //    includeEventTypes.Add(JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_BOSS);



        //    if (configs.Settings.Instence().GWEventMgrMapExportMode == configs.UIConst.GW_EVENT_MGR_EXPORT_MAP_MODE_ALL)
        //    {
        //        for (int i = 1; i <= 55; i++) rows.Add(i);
        //        foreach (var type in includeEventTypes)
        //        {
        //            var curData = configs.Core.Instence().Context.GroupWarDataM.QueryEventMgrMapList(type, area, true);
        //            foreach (var item in curData)
        //            {
        //                var row = JSONHelper.ParseInt(item.row);
        //                var col = JSONHelper.ParseInt(item.col);
        //                if (type != JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_BOSS) focusPoses.Add((row, col));
        //            }
        //        }
        //    }
        //    else if (configs.Settings.Instence().GWEventMgrMapExportMode == configs.UIConst.GW_EVENT_MGR_EXPORT_MAP_MODE_WITH_REFERENCE)
        //    {
        //        var extraRows = new List<int>();
        //        var extraNotIncludeRows = new List<int>();
        //        var tempRows = new List<int> { 52, 53, 54, 55 };
        //        var allEventTypes = new List<int>
        //        {
        //            JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_MINE,
        //        JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_MONSTER,
        //        JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_SINGLE,
        //        JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_MULTI,
        //    };

        //        foreach (var type in includeEventTypes)
        //        {
        //            var curData = configs.Core.Instence().Context.GroupWarDataM.QueryEventMgrMapList(type, area, true);
        //            foreach (var item in curData)
        //            {
        //                var row = JSONHelper.ParseInt(item.row);
        //                var col = JSONHelper.ParseInt(item.col);
        //                if (!rows.Contains(row)) rows.Add(row);
        //                if (type != JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_BOSS) focusPoses.Add((row, col));
        //                // 事件相关上下三层
        //                for (int i = Math.Max(1, row - 2); i <= Math.Min(row + 2, 55); i++)
        //                {
        //                    if (!tempRows.Contains(i)) tempRows.Add(i);
        //                }
        //            }
        //        }
        //        foreach (var type in allEventTypes)
        //        {
        //            var curData = configs.Core.Instence().Context.GroupWarDataM.QueryEventMgrMapList(type, area, true);
        //            foreach (var item in curData)
        //            {
        //                var row = JSONHelper.ParseInt(item.row);
        //                if (!includeEventTypes.Contains(type) && !extraNotIncludeRows.Contains(row)) extraNotIncludeRows.Add(row);
        //                if (!extraRows.Contains(row)) extraRows.Add(row);

        //            }
        //        }

        //        tempRows.Sort((a, b) => a - b);
        //        // 切片row
        //        var tempGroupRows = new Dictionary<int, List<int>>();
        //        int curKey = 0;
        //        for (int i = 0; i < tempRows.Count; i++)
        //        {
        //            if (i == 0)
        //            {
        //                if (!tempGroupRows.ContainsKey(curKey)) tempGroupRows.Add(curKey, new List<int>());
        //                tempGroupRows[curKey].Add(tempRows[i]);
        //            }
        //            else
        //            {
        //                if (tempRows[i] - tempRows[i - 1] > 1) curKey++;
        //                if (!tempGroupRows.ContainsKey(curKey)) tempGroupRows.Add(curKey, new List<int>());
        //                tempGroupRows[curKey].Add(tempRows[i]);
        //            }
        //        }

        //        // 检查每个分组内的事件数量
        //        foreach (var group in tempGroupRows)
        //        {
        //            var found = 0;
        //            var foundRow = 0;
        //            foreach (var item in group.Value)
        //            {
        //                if (rows.Contains(item) || extraNotIncludeRows.Contains(item))
        //                {
        //                    found++;
        //                    foundRow = item;
        //                }
        //            }

        //            // 该组只有一个事件，需要从其他事件中捞
        //            if (found <= 1 && foundRow > 0)
        //            {
        //                var minDistance = -1;
        //                var selectRow = 0;
        //                foreach (var row in extraRows)
        //                {
        //                    if (row == foundRow) continue;
        //                    if (minDistance == -1)
        //                    {
        //                        minDistance = Math.Abs(row - foundRow);
        //                        selectRow = row;
        //                    }
        //                    else
        //                    {
        //                        var curDistance = Math.Abs(row - foundRow);
        //                        if (curDistance < minDistance)
        //                        {
        //                            minDistance = curDistance;
        //                            selectRow = row;
        //                        }
        //                    }
        //                }

        //                if (minDistance > 0)
        //                {
        //                    for (int i = Math.Max(1, selectRow - 2); i <= Math.Min(selectRow + 2, 55); i++)
        //                    {
        //                        if (!tempRows.Contains(i)) tempRows.Add(i);
        //                    }
        //                }
        //            }
        //        }

        //        rows = tempRows;

        //        rows.Sort((a, b) => a - b);
        //        rows = ConvertMapRowList(rows);
        //    }
        //    else
        //    {
        //        for (int i = 52; i <= 55; i++) rows.Add(i);
        //        foreach (var type in includeEventTypes)
        //        {
        //            var curData = configs.Core.Instence().Context.GroupWarDataM.QueryEventMgrMapList(type, area, true);
        //            foreach (var item in curData)
        //            {
        //                var row = JSONHelper.ParseInt(item.row);
        //                var col = JSONHelper.ParseInt(item.col);
        //                if (type != JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_BOSS) focusPoses.Add((row, col));
        //                // 事件相关上下三层
        //                for (int i = Math.Max(1, row - 2); i <= Math.Min(row + 2, 55); i++)
        //                {
        //                    if (!rows.Contains(i)) rows.Add(i);
        //                }
        //            }
        //        }
        //        rows.Sort((a, b) => a - b);
        //        rows = ConvertMapRowList(rows);
        //    }


        //    #endregion

        //    #region PREPARE MAP DATA
        //    var mapData = configs.Core.Instence().Context.GroupWarDataM.QueryEventMapGrids(area, rows);
        //    var backgrounData = configs.Core.Instence().Context.GroupWarDataM.QueryEventMapBg();
        //    #endregion

        //    #region BACKGROUND
        //    int ratio = 2;
        //    int totalHeight = (335 + rows.Count * 50 + 55) * ratio;
        //    int totalWidth = 510 * ratio;
        //    var b = new System.Drawing.Bitmap(totalWidth, totalHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        //    var g = Graphics.FromImage(b);
        //    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        //    g.Clear(Color.Transparent);

        //    // 头图
        //    var icon = BitmapHelper.BitmapSourceToBitmap(BitmapHelper.LoadResource("gw/event_export_template_map_1"));
        //    g.DrawImage(icon, new Rectangle(0, 0, totalWidth, 335 * ratio), new RectangleF(0, 0, icon.Width, icon.Height), GraphicsUnit.Pixel);

        //    // 物种背景 (填充)
        //    var iconSize = new Size(0, 0);
        //    var iconPos = new Point(0, 0);

        //    icon = backgrounData.isPacked ? LoadFilePacked(backgrounData.path) : LoadFile(backgrounData.path);
        //    if (icon is not null)
        //    {
        //        int fillHeight = rows.Count * 50 * ratio;
        //        iconSize = icon.Size;
        //        ScaleWHByTwoLimit(ref iconSize, 500 * ratio, 0);
        //        iconPos.X = 5 * ratio;
        //        iconPos.Y = 335 * ratio;
        //        do
        //        {
        //            if (iconSize.Height > fillHeight)
        //            {
        //                var actualHeight = Convert.ToInt32((double)icon.Height * ((double)fillHeight / (double)iconSize.Height));
        //                g.DrawImage(icon, new Rectangle(iconPos.X, iconPos.Y, iconSize.Width, fillHeight),
        //                    new RectangleF(0, 0, icon.Width, actualHeight), GraphicsUnit.Pixel);
        //                break;
        //            }
        //            else
        //            {
        //                g.DrawImage(icon, new Rectangle(iconPos.X, iconPos.Y, iconSize.Width, iconSize.Height),
        //                  new RectangleF(0, 0, icon.Width, icon.Height), GraphicsUnit.Pixel);
        //                fillHeight -= iconSize.Height;
        //                iconPos.Y += iconSize.Height;
        //            }
        //        } while (true);
        //    }
        //    #endregion

        //    #region TITLE
        //    var commonBrush = new SolidBrush(System.Drawing.Color.FromArgb(80, 80, 80));
        //    var whiteBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 255, 255));
        //    var redBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 67, 67));
        //    System.Drawing.Pen pen = new System.Drawing.Pen(redBrush, 3 * ratio);
        //    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;

        //    // AREA #01
        //    var title = $"#{area:00}";
        //    iconPos.X = 40 * ratio;
        //    iconPos.Y = 195 * ratio;
        //    DrawString(g, iconPos, title, FontHelper.FontWeight.Bold, 87, commonBrush, StringAlignment.LEFT);

        //    // CLUB INFO
        //    var titles = configs.Core.Instence().Context.GroupWarDataM.GetGroupEventThumTitleInfo2();
        //    iconPos.X = 180 * ratio;
        //    iconPos.Y = 185 * ratio;
        //    DrawString(g, iconPos, titles[0], FontHelper.FontWeight.Bold, 22, commonBrush, StringAlignment.LEFT);
        //    iconPos.Y = 207 * ratio;
        //    DrawString(g, iconPos, titles[1], FontHelper.FontWeight.Black, 22, commonBrush, StringAlignment.LEFT);
        //    iconPos.Y = 229 * ratio;
        //    DrawString(g, iconPos, titles[2], FontHelper.FontWeight.Bold, 22, commonBrush, StringAlignment.LEFT);
        //    #endregion

        //    #region MAP GRIDS
        //    int bgMargin = -1 * ratio;
        //    int iconMargin = 4 * ratio;
        //    int circleMargin = -15 * ratio;
        //    var circles = new List<(Rectangle circle, int row, int col)>();
        //    var hideOtherGrids = configs.Settings.Instence().GWEventMgrMapExportHideOtherBlocks;
        //    foreach (var rowData in mapData)
        //    {
        //        var row = JSONHelper.ParseInt(rowData.Name);
        //        foreach (var colData in rowData.Value)
        //        {
        //            var col = JSONHelper.ParseInt(colData.Name);
        //            var gridData = colData.Value;

        //            var rule = JSONHelper.ParseString(gridData.rule);
        //            if (hideOtherGrids && (rule == "EMPTY" || rule == "DRILL" || rule == "BOX")) continue;

        //            var size = JSONHelper.ParseInt(gridData.size, 1);
        //            var offsetX = JSONHelper.ParseInt(gridData["offset_x"]);
        //            var offsetY = JSONHelper.ParseInt(gridData["offset_y"]);
        //            iconSize.Width = size > 1 ? 100 * ratio : 50 * ratio;
        //            iconSize.Height = size > 2 ? 100 * ratio : 50 * ratio;
        //            iconPos.X = 5 * ratio + (col - offsetX) * 50 * ratio;
        //            iconPos.Y = 335 * ratio + (rows.IndexOf(row) - offsetY) * 50 * ratio;

        //            // BG
        //            string path = JSONHelper.ParseString(gridData.bg);
        //            var isPacked = JSONHelper.ParseBool(gridData.bg_packed);
        //            if (!string.IsNullOrEmpty(path))
        //            {
        //                icon = isPacked ? LoadFilePacked(path) : LoadFile(path);
        //                if (icon is not null)
        //                {
        //                    g.DrawImage(icon, new Rectangle(iconPos.X + bgMargin, iconPos.Y + bgMargin, iconSize.Width - 2 * bgMargin, iconSize.Height - 2 * bgMargin), new RectangleF(0, 0, icon.Width, icon.Height), GraphicsUnit.Pixel);
        //                }
        //            }
        //            // ICON 居中
        //            path = JSONHelper.ParseString(gridData.icon);
        //            isPacked = JSONHelper.ParseBool(gridData.icon_packed);
        //            if (!string.IsNullOrEmpty(path))
        //            {
        //                if (path.Contains("mine") || path.Contains("multi")) iconMargin = 8 * ratio;
        //                else iconMargin = 2 * ratio;

        //                icon = isPacked ? LoadFilePacked(path) : LoadFile(path);
        //                if (icon is not null)
        //                {
        //                    g.DrawImage(icon, new Rectangle(iconPos.X + iconMargin, iconPos.Y + iconMargin, iconSize.Width - 2 * iconMargin, iconSize.Height - 2 * iconMargin), new RectangleF(0, 0, icon.Width, icon.Height), GraphicsUnit.Pixel);
        //                }
        //            }
        //            // MARK L
        //            path = JSONHelper.ParseString(gridData.mark_l);
        //            isPacked = JSONHelper.ParseBool(gridData.mark_l_packed);
        //            if (!string.IsNullOrEmpty(path))
        //            {
        //                icon = isPacked ? LoadFilePacked(path) : LoadFile(path);
        //                if (icon is not null)
        //                {
        //                    g.DrawImage(icon, new Rectangle(iconPos.X + 29 * ratio + (size > 1 ? 50 * ratio : 0), iconPos.Y + 30 * ratio, 10 * ratio, 14 * ratio), new RectangleF(0, 0, icon.Width, icon.Height), GraphicsUnit.Pixel);
        //                }
        //            }
        //            // MARK R
        //            path = JSONHelper.ParseString(gridData.mark_r);
        //            isPacked = JSONHelper.ParseBool(gridData.mark_r_packed);
        //            if (!string.IsNullOrEmpty(path))
        //            {
        //                icon = isPacked ? LoadFilePacked(path) : LoadFile(path);
        //                if (icon is not null)
        //                {
        //                    g.DrawImage(icon, new Rectangle(iconPos.X + 37 * ratio + (size > 1 ? 50 * ratio : 0), iconPos.Y + 30 * ratio, 10 * ratio, 14 * ratio), new RectangleF(0, 0, icon.Width, icon.Height), GraphicsUnit.Pixel);
        //                }
        //            }

        //            // 画圈
        //            foreach (var focus in focusPoses)
        //            {
        //                if (focus.row == row && focus.col == col)
        //                {
        //                    //    g.DrawEllipse(pen, new Rectangle(iconPos.X + circleMargin, iconPos.Y + circleMargin, iconSize.Width - 2 * circleMargin, iconSize.Height - 2 * circleMargin));
        //                    circles.Add((new Rectangle(iconPos.X + circleMargin, iconPos.Y + circleMargin, iconSize.Width - 2 * circleMargin, iconSize.Height - 2 * circleMargin), focus.row, focus.col));
        //                    break;
        //                }
        //            }
        //        }
        //    }

        //    if (circles.Count > 0)
        //    {
        //        foreach (var item in circles)
        //        {
        //            g.DrawEllipse(pen, item.circle);
        //            title = string.Format(StringHelper.FixContent(langs.ResourceLang._gw_area_row_col), area, item.row, item.col);
        //            iconPos.X = (50 + item.col * 50) * ratio;
        //            iconPos.Y = 400 * ratio + (rows.IndexOf(item.row)) * 50 * ratio;
        //            DrawStringWitBorder(g, iconPos, title, FontHelper.FontWeight.Bold, 30, whiteBrush, commonBrush, 2 * ratio, StringAlignment.CENTER);
        //        }

        //    }

        //    #endregion

        //    #region SIDE GRIDS
        //    // 每行的格子 （加数字）
        //    iconPos.X = 25 * ratio;
        //    iconPos.Y = 335 * ratio;
        //    icon = BitmapHelper.BitmapSourceToBitmap(BitmapHelper.LoadResource("gw/event_export_template_map_2"));
        //    for (int i = 0; i < rows.Count; i++)
        //    {
        //        g.DrawImage(icon, new Rectangle(0, iconPos.Y, totalWidth, 50 * ratio), new RectangleF(0, 0, icon.Width, icon.Height), GraphicsUnit.Pixel);
        //        iconPos.Y += 35;
        //        DrawString(g, iconPos, rows[i] == -1 ? "~" : rows[i].ToString(), FontHelper.FontWeight.Bold, 40, whiteBrush, StringAlignment.CENTER);
        //        iconPos.Y -= 35;
        //        iconPos.Y += 50 * ratio;
        //    }

        //    icon = BitmapHelper.BitmapSourceToBitmap(BitmapHelper.LoadResource("gw/event_export_template_map_3"));
        //    g.DrawImage(icon, new Rectangle(0, iconPos.Y, totalWidth, 50 * ratio), new RectangleF(0, 0, icon.Width, icon.Height), GraphicsUnit.Pixel);
        //    var curY = iconPos.Y + 30;
        //    for (int i = 1; i <= 9; i++)
        //    {
        //        iconPos.X = 25 * ratio + i * 50 * ratio;
        //        iconPos.Y = 305 * ratio;
        //        DrawString(g, iconPos, i.ToString(), FontHelper.FontWeight.Bold, 40, whiteBrush, StringAlignment.CENTER);
        //        iconPos.Y = curY;
        //        DrawString(g, iconPos, i.ToString(), FontHelper.FontWeight.Bold, 40, whiteBrush, StringAlignment.CENTER);
        //    }
        //    #endregion

        //    return b;
        //}
        #endregion

        #region UTILS
        private static List<int> ConvertMapRowList(List<int> data)
        {
            var ret = new List<int>();
            if (data.Count <= 0) return ret;
            //    if (data[0] != 1) ret.Add(-1);
            for (int i = 0; i < data.Count; i++)
            {
                if (i > 0)
                {
                    if (data[i] - data[i - 1] != 1)
                    {
                        ret.Add(-1);
                    }
                }
                ret.Add(data[i]);
            }
            return ret;
        }
        private static string GetGWGridShortName(string name)
        {
            var useful = "1234567890αβγ军团軍團";
            var ret = "";
            for (int i = 0; i < name.Length; i++)
            {
                var cur = name[i];
                if (useful.Contains(cur)) ret += cur;
            }
            return ret;
        }
        /// <summary>
        /// 中心点位置转左边点位置
        /// </summary>
        /// <param name="center"></param>
        /// <param name="selfLength"></param>
        /// <returns></returns>
        private static int CenterPos2EdgePos(int center, int selfLength) => Convert.ToInt32(Math.Ceiling((double)center - (double)selfLength / 2d));
        /// <summary>
        /// 按照一个最大数值缩放宽高尺寸
        /// </summary>
        /// <param name="size"></param>
        /// <param name="limit"></param>
        private static void ScaleWHByOneLimit(ref Size size, int limit)
        {
            double scale = 0;
            if (size.Width > size.Height)
            {
                if (size.Width != limit)
                {
                    scale = (double)limit / (double)size.Width;
                    size.Width = limit;
                    size.Height = Convert.ToInt32(Math.Ceiling(size.Height * scale));
                }
            }
            else
            {
                if (size.Height != limit)
                {
                    scale = (double)limit / (double)size.Height;
                    size.Height = limit;
                    size.Width = Convert.ToInt32(Math.Ceiling((double)size.Width * scale));
                }
            }
        }
        /// <summary>
        /// 按照宽高指定最大值进行缩放
        /// </summary>
        /// <param name="size"></param>
        /// <param name="widthLimit"></param>
        /// <param name="heightLimit"></param>
        private static void ScaleWHByTwoLimit(ref Size size, int widthLimit, int heightLimit)
        {
            if (!(heightLimit > 0 ^ widthLimit > 0)) return;
            double scale = 0;
            if (widthLimit > 0)
            {
                if (size.Width != widthLimit)
                {
                    scale = (double)widthLimit / (double)size.Width;
                    size.Width = widthLimit;
                    size.Height = Convert.ToInt32(Math.Round((double)size.Height * scale));
                }
            }
            else if (heightLimit > 0)
            {
                if (size.Height != heightLimit)
                {
                    scale = (double)heightLimit / (double)size.Height;
                    size.Height = heightLimit;
                    size.Width = Convert.ToInt32(Math.Round((double)size.Width * scale));
                }
            }
        }
        /// <summary>
        /// 绘制文字
        /// </summary>
        /// <param name="g">图片对象</param>
        /// <param name="pos">位置</param>
        /// <param name="str">字符串</param>
        /// <param name="fontWeight">字重</param>
        /// <param name="fontSize">字号</param>
        /// <param name="brush">颜色</param>
        /// <param name="alignment">对齐方式</param>
        /// <returns>本行结尾X坐标</returns>
        public static int DrawString(Graphics g, Point pos, string str, FontHelper.FontWeight fontWeight, double fontSize, Brush brush, StringAlignment alignment)
        {
            var font = FontHelper.GetFont(fontWeight, fontSize);
            var size = g.MeasureString(str, font, new PointF() { X = pos.X, Y = pos.Y }, StringFormat.GenericTypographic);

            size.Height = Convert.ToInt32(fontSize);
            if (alignment == StringAlignment.CENTER) pos.X -= Convert.ToInt32(Math.Round(size.Width / 2d));
            else if (alignment == StringAlignment.RIGHT) pos.X -= Convert.ToInt32(size.Width);

            pos.Y -= Convert.ToInt32(Math.Round(size.Height / 2d));

            g.DrawString(str, font, brush, pos);

            return pos.X + Convert.ToInt32(size.Width);
        }
        /// <summary>
        /// 绘制带有描边的字体
        /// </summary>
        /// <param name="g">图片</param>
        /// <param name="pos">位置</param>
        /// <param name="str">字符串</param>
        /// <param name="fontWeight">字重</param>
        /// <param name="fontSize">字号</param>
        /// <param name="fillBrush">填充颜色</param>
        /// <param name="borderBrush">描边颜色</param>
        /// <param name="borderWidth">描边宽度</param>
        /// <param name="alignment">对齐方式</param>
        /// <returns>本行结尾X坐标</returns>
        public static int DrawStringWitBorder(Graphics g, Point pos, string str, FontHelper.FontWeight fontWeight, double fontSize, Brush fillBrush, Brush borderBrush, int borderWidth, StringAlignment alignment)
        {
            var font = FontHelper.GetFont(fontWeight, fontSize);
            var size = g.MeasureString(str, font, new PointF() { X = pos.X, Y = pos.Y }, StringFormat.GenericTypographic);

            if (alignment == StringAlignment.CENTER) pos.X -= Convert.ToInt32(Math.Round(size.Width / 2));
            else if (alignment == StringAlignment.RIGHT) pos.X -= Convert.ToInt32(size.Width);

            // pos.Y -= Convert.ToInt32(Math.Round(size.Height / 2d));
            pos.X -= Convert.ToInt32(Math.Round(borderWidth / 2d));
            pos.Y -= Convert.ToInt32(Math.Round(borderWidth / 2d));

            var border = new GraphicsPath();
            border.AddString(str, font.FontFamily, (int)font.Style, font.Size, pos, StringFormat.GenericTypographic);
            g.DrawPath(new Pen(borderBrush, borderWidth), border);
            g.FillPath(fillBrush, border);

            return pos.X + Convert.ToInt32(size.Width);
        }


        #endregion
        #region ENUMS
        public enum StringAlignment
        {
            LEFT = 0,
            RIGHT = 2,
            CENTER = 3,
        }
        #endregion

        #region FILES
        public static Bitmap ByteToBitmap(byte[] data)
        {
            if (data is null || data.Length == 0) return null;
            try
            {
                using (var mem = new MemoryStream(data))
                {
                    return (Bitmap)Image.FromStream(mem, true);
                }
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, "DrawingHelper.ByteToBitmap");
            }
            return null;
        }
        /// <summary>
        /// 从文件加载图片
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Bitmap LoadFile(string fileName)
        {
            if (!fileName.Contains(@$"RES\IMG\")) fileName = @$"RES\IMG\{fileName}";
            if (!fileName.EndsWith(".png")) fileName += ".png";
            return ByteToBitmap(CryptoHelper.PNGLoad(fileName));
        }
        private static Bitmap GetResourcePNG(string name, string folder = "handbook") => LoadFile($"RES\\IMG\\jjj\\{folder}\\{name}");

        /// <summary>
        /// 编辑Bitmap图像
        /// </summary>
        /// <param name="source">原图像</param>
        /// <param name="targetWidth">目标宽</param>
        /// <param name="targetHeight">目标高</param>
        /// <param name="targetOffsetX">目标X偏移</param>
        /// <param name="targetOffsetY">目标Y偏移</param>
        /// <param name="sourceScale">原图像缩放</param>
        /// <param name="flitX">左右翻转</param>
        /// <param name="flitY">上下翻转</param>
        /// <returns></returns>
        public static Bitmap EditBitmap(Bitmap source, int targetWidth, int targetHeight, int targetOffsetX = 0, int targetOffsetY = 0, double sourceScale = 1, bool flitX = false, bool flitY = false)
        {
            if (source is null) return null;
            var b = new System.Drawing.Bitmap(targetWidth, targetHeight);
            var g = Graphics.FromImage(b);
            g.Clear(Color.Transparent);

            if (flitX) source.RotateFlip(RotateFlipType.Rotate180FlipY);
            if (flitY) source.RotateFlip(RotateFlipType.RotateNoneFlipY);


            var sourceHeight = source.Height;
            var sourceWidth = source.Width;
            var scaleSourceHeight = Convert.ToInt32(sourceHeight * sourceScale);
            var scaleSourceWidth = Convert.ToInt32(sourceWidth * sourceScale);
            int targetX = (targetWidth - scaleSourceHeight) / 2;
            int targetY = (targetHeight - scaleSourceHeight) / 2;

            g.DrawImage(source,
                        new Rectangle(targetX, targetY, scaleSourceWidth, scaleSourceHeight),
                        new Rectangle(targetOffsetX, targetOffsetX, sourceWidth, sourceHeight),
                        GraphicsUnit.Pixel);
            return b;
        }

        #region PACKED IMAGES
        private static Dictionary<string, Bitmap> _slideBitmapCache = new Dictionary<string, Bitmap>();
        public static Bitmap LoadFilePacked(string path)
        {
            if (_slideBitmapCache.ContainsKey(path)) return _slideBitmapCache[path];

            var arr = path.Split("\\");
            var pngPath = string.Join("\\", arr[..^1]) + ".png";
            var plistPath = string.Join("\\", arr[..^1]) + ".plist";
            //var key = arr.Last().Replace(".png", "");
            if (!File.Exists(pngPath) || !File.Exists(plistPath)) return null;
            SlidePackedPng(pngPath, plistPath);
            if (_slideBitmapCache.ContainsKey(path)) return _slideBitmapCache[path];
            return null;
        }


        public static void SlidePackedPng(string pngPath, string plistPath)
        {
            try
            {
                var source_bitmap = LoadFile(pngPath);
                if (source_bitmap == null) return;

                var folderPath = Path.GetDirectoryName(pngPath) + "\\" + Path.GetFileNameWithoutExtension(pngPath);
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
                var meta = PlistHelper.PlistToJToken(plistPath, true);
                if (meta == null) return;
                if (meta == null) return;
                string itemPath = "";
                JToken jv = null;

                Bitmap b = null;
                Graphics g = null;

                int metaWidth = 0;
                int metaHeight = 0;

                // metadata
                var data = meta["texture"];
                if (data is not null)
                {
                    metaWidth = JSONHelper.ParseInt(data["width"]);
                    metaHeight = JSONHelper.ParseInt(data["height"]);
                }

                data = meta["frames"];
                if (data is not null)
                {
                    foreach (var frame in data)
                    {
                        itemPath = frame.Name;
                        itemPath = folderPath + "\\" + itemPath.Replace("/", "\\").Split("\\").Last();

                        var info = frame.Value;
                        string str = JSONHelper.ParseString(info["frame"]);
                        if (string.IsNullOrEmpty(str)) continue;
                        var frameInfo = ConvertFrameInfo(str);

                        str = JSONHelper.ParseString(info["sourceSize"]);
                        if (string.IsNullOrEmpty(str)) continue;
                        var sourceSize = ConvertSize(str);

                        var rotated = JSONHelper.ParseBool(info["rotated"]);
                        var offset = ConvertPoint(JSONHelper.ParseString(info["offset"]));

                        var width = sourceSize.Width;
                        var height = sourceSize.Height;

                        if (rotated)
                        {
                            offset.X += (sourceSize.Height - frameInfo.Item2.Height) / 2;
                            offset.Y += (sourceSize.Width - frameInfo.Item2.Width) / 2;
                            b = new Bitmap(height, width, PixelFormat.Format32bppArgb); //这个地方是做透明处理结果的
                            g = Graphics.FromImage(b);
                            g.Clear(Color.Transparent);
                            g.DrawImage(source_bitmap,
                                        new Rectangle(0, 0, sourceSize.Height, sourceSize.Width),
                                        new Rectangle(frameInfo.startPoint.X, frameInfo.startPoint.Y, frameInfo.Item2.Height, frameInfo.Item2.Width),
                                        GraphicsUnit.Pixel);
                            b.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        }
                        else
                        {
                            offset.X += (sourceSize.Width - frameInfo.Item2.Width) / 2;
                            offset.Y += (sourceSize.Height - frameInfo.Item2.Height) / 2;
                            b = new Bitmap(width, height, PixelFormat.Format32bppArgb); //这个地方是做透明处理结果的
                            g = Graphics.FromImage(b);
                            g.Clear(Color.Transparent);
                            g.DrawImage(source_bitmap,
                                        new Rectangle(0, 0, sourceSize.Width, sourceSize.Height),
                                        new Rectangle(frameInfo.startPoint.X, frameInfo.startPoint.Y, frameInfo.Item2.Width, frameInfo.Item2.Height),
                                        GraphicsUnit.Pixel);
                        }
                        _slideBitmapCache.TryAdd(itemPath, b);
                    }
                }
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, TAG);
            }
        }
        private static (Point startPoint, Size imageSize) ConvertFrameInfo(string str)
        {
            var def = (new Point(0, 0), new Size(0, 0));
            if (string.IsNullOrEmpty(str)) return def;
            str = str.Replace("(", "").Replace(")", "").Replace(",,", ",");
            var arr = str.Split(",");
            if (arr.Length != 4) return def;
            try
            {
                return (new Point(int.Parse(arr[0]), int.Parse(arr[1])),
                        new Size(int.Parse(arr[2]), int.Parse(arr[3])));
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, TAG);
                return def;
            }
        }

        private static Size ConvertSize(string str)
        {
            var def = new Size(0, 0);
            if (string.IsNullOrEmpty(str)) return def;
            str = str.Replace("(", "").Replace(")", "").Replace(",,", ",");
            var arr = str.Split(",");
            if (arr.Length != 2) return def;
            try
            {
                return new Size(int.Parse(arr[0]), int.Parse(arr[1]));
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, TAG);
                return def;
            }
        }
        private static Point ConvertPoint(string str)
        {
            var def = new Point(0, 0);
            if (string.IsNullOrEmpty(str)) return def;
            str = str.Replace("(", "").Replace(")", "").Replace(",,", ",");
            var arr = str.Split(",");
            if (arr.Length != 2) return def;
            try
            {
                return new Point(Convert.ToInt32(Convert.ToDouble(arr[0])), Convert.ToInt32(Convert.ToDouble(arr[1])));
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, TAG);
                return def;
            }
        }
        #endregion
        #endregion
    }
}
