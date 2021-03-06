﻿using System;
using System.Text;

namespace TweetDck.Core.Notification{
    sealed class TweetNotification{
        private static string FontSizeClass { get; set; }
        private static string HeadTag { get; set; }

        private const string DefaultFontSizeClass = "medium";
        private const string DefaultHeadTag = @"<meta charset='utf-8'><meta http-equiv='X-UA-Compatible' content='chrome=1'><link rel='stylesheet' href='https://ton.twimg.com/tweetdeck-web/web/css/font.5ef884f9f9.css'><link rel='stylesheet' href='https://ton.twimg.com/tweetdeck-web/web/css/app-dark.5631e0dd42.css'>";

        private const string FixedCSS = @"a[data-full-url]{word-break:break-all}.txt-base-smallest .badge-verified:before{height:13px!important}";
        private const string CustomCSS = @".scroll-styled-v::-webkit-scrollbar{width:8px}.scroll-styled-v::-webkit-scrollbar-thumb{border-radius:0}#td-skip{opacity:0;cursor:pointer;transition:opacity 0.15s ease}.td-hover #td-skip{opacity:0.75}#td-skip:hover{opacity:1}";

        public static int FontSizeLevel{
            get{
                switch(FontSizeClass){
                    case "largest": return 4;
                    case "large": return 3;
                    case "medium": return 2;
                    case "small": return 1;
                    default: return 0;
                }
            }
        }

        public static TweetNotification ExampleTweet{
            get{
                StringBuilder build = new StringBuilder();
                build.Append(@"<article><div class='js-stream-item-content item-box js-show-detail'><div class='js-tweet tweet'>");
                build.Append(@"<header class='tweet-header'>");
                build.Append(@"<time class='tweet-timestamp js-timestamp pull-right txt-mute'><a target='_blank' rel='url' href='https://twitter.com/chylexmc' class='txt-small'>0s</a></time>");
                build.Append(@"<a target='_blank' rel='user' href='https://twitter.com/chylexmc' class='account-link link-complex block'>");
                build.Append(@"<div class='obj-left item-img tweet-img'><img width='48' height='48' alt='chylexmc's avatar' src='https://pbs.twimg.com/profile_images/765161905312980992/AhDP9iY-_normal.jpg' class='tweet-avatar avatar pull-right'></div>");
                build.Append(@"<div class='nbfc'><span class='account-inline txt-ellipsis'><b class='fullname link-complex-target'>chylex</b> <span class='username txt-mute'>@chylexmc</span></span></div>");
                build.Append(@"</a>");
                build.Append(@"</header>");
                build.Append(@"<div class='tweet-body'><p class='js-tweet-text tweet-text with-linebreaks'>This is an example tweet, which lets you test the location and duration of popup notifications.</p></div>");
                
                #if DEBUG
                build.Append(@"<div style='margin-top:64px'>Scrollbar test padding...</div>");
                #endif

                build.Append(@"</div></div></article>");

                return new TweetNotification(build.ToString(), "", 95, true);
            }
        }

        public static void SetFontSizeClass(string newFSClass){
            FontSizeClass = newFSClass;
        }

        public static void SetHeadTag(string headContents){
            HeadTag = headContents;
        }

        public enum Position{
            TopLeft, TopRight, BottomLeft, BottomRight, Custom
        }

        public string Url{
            get{
                return url;
            }
        }

        private readonly string html;
        private readonly string url;
        private readonly int characters;
        private readonly bool isExample;

        public TweetNotification(string html, string url, int characters) : this(html, url, characters, false){}

        private TweetNotification(string html, string url, int characters, bool isExample){
            this.html = html;
            this.url = url;
            this.characters = characters;
            this.isExample = isExample;
        }

        public int GetDisplayDuration(int value){
            return 2000+Math.Max(1000, value*characters);
        }

        public string GenerateHtml(string bodyClasses = null, bool enableCustomCSS = true){
            StringBuilder build = new StringBuilder();
            build.Append("<!DOCTYPE html>");
            build.Append("<html class='os-windows txt-base-").Append(FontSizeClass ?? DefaultFontSizeClass).Append("'>");
            build.Append("<head>").Append(HeadTag ?? DefaultHeadTag);
            
            if (enableCustomCSS){
                build.Append("<style type='text/css'>").Append(FixedCSS).Append(CustomCSS).Append("</style>");

                if (!string.IsNullOrEmpty(Program.UserConfig.CustomNotificationCSS)){
                    build.Append("<style type='text/css'>").Append(Program.UserConfig.CustomNotificationCSS).Append("</style>");
                }
            }
            else{
                build.Append("<style type='text/css'>").Append(FixedCSS).Append("</style>");
            }
            
            build.Append("</head>");
            build.Append("<body class='hearty");

            if (!string.IsNullOrEmpty(bodyClasses)){
                build.Append(' ').Append(bodyClasses);
            }

            build.Append('\'').Append(isExample ? " td-example-notification" : "").Append("><div class='app-columns-container'><div class='column scroll-styled-v' style='width:100%;overflow-y:auto'>");
            build.Append(html);
            build.Append("</div></div></body>");
            build.Append("</html>");
            return build.ToString();
        }
    }
}
