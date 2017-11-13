using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Web;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using Google.Apis.Customsearch.v1;
using Google.Apis.Customsearch.v1.Data;
using Google.Apis.Services;
using HtmlAgilityPack;

namespace Google_Keyword_Ranker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private static string API_KEY = "AIzaSyAYWbHusC1aSia2x_PYL54Sgp7XdeqnMUc";

        //The custom search engine identifier
        private static string cx = "015598178761323117960:sbbkk2__0lo";

        public static CustomsearchService Service = new CustomsearchService(
            new BaseClientService.Initializer
            {
                ApplicationName = "Google-Keyword-Ranker",
                ApiKey = API_KEY,
            });

        public static IList<Result> Search(string query)
        {
            CseResource.ListRequest listRequest = Service.Cse.List(query);
            listRequest.Cx = cx;

            Search search = listRequest.Execute();
            return search.Items;
        }
        

        private void btnGo_Click(object sender, RoutedEventArgs e)
        {
            listKeyword.Items.Clear();
            tbSearch.IsEnabled = btnGo.IsEnabled = false;
            this.Dispatcher.Invoke(() =>
            {
                search();
            });
        }

        public class KeywordItem
        {
            public string Keyword { get; set; }
            public int Frequency { get; set; }
        }

        void addCollection(string str)
        {
            if (str.Trim() == "")
            {
                return;
            }
            str = str.Trim();
            foreach (var item in listKeyword.Items)
            {
                var keyword = (KeywordItem) item;
                if (keyword.Keyword == str)
                {
                    keyword.Frequency += 1;
                    return;
                }
            }
            listKeyword.Items.Add(new KeywordItem { Keyword = str, Frequency = 1});
        }

        private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is GridViewColumnHeader)
            {
                //Get clicked column
                GridViewColumn clickedColumn = (e.OriginalSource as GridViewColumnHeader).Column;
                if (clickedColumn != null)
                {
                    //Get binding property of clicked column
                    string bindingProperty = (clickedColumn.DisplayMemberBinding as Binding).Path.Path;
                    SortDescriptionCollection sdc = listKeyword.Items.SortDescriptions;
                    ListSortDirection sortDirection = ListSortDirection.Ascending;
                    if (sdc.Count > 0)
                    {
                        SortDescription sd = sdc[0];
                        sortDirection = (ListSortDirection)((((int)sd.Direction) + 1) % 2);
                        sdc.Clear();
                    }
                    sdc.Add(new SortDescription(bindingProperty, sortDirection));
                }
            }
        }


        private String[] metas = new String[]
        {
            "//meta[@name='description']",
            "//meta[@name='keywords']"
        };

        private String[] tags = new String[]
        {
            "h1",
            "h2",
            "h3",
            "title"
        };

        string getHTML(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage response = client.GetAsync(url).Result)
                {
                    using (HttpContent content = response.Content)
                    {
                        return content.ReadAsStringAsync().Result;
                    }
                }
            }
        }

        private void search ()
        {
            string query = tbSearch.Text;
            var results = Search(query);
            foreach (Result result in results)
            {
                var web = new HtmlWeb();
                HtmlDocument doc = web.Load(result.Link);
                foreach (string metaname in metas)
                {

                    try
                    {
                        var nodes = doc.DocumentNode.SelectNodes(metaname);
                        foreach (var mdnode in nodes)
                        {
                            if (mdnode != null)
                            {
                                HtmlAttribute desc;
                                foreach (var str in mdnode.Attributes)
                                {
                                    String[] words = str.Value.Split(' ', ',');
                                    foreach (string word in words)
                                    {
                                        addCollection(word);
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                    }
                }
                foreach (string tagname in tags)
                {
                    try
                    {
                        var nodes = doc.DocumentNode.Descendants(tagname).ToList();
                        foreach (HtmlNode mdnode in nodes)
                        {
                            String[] words = mdnode.InnerText.Split(' ', ',');
                            foreach (string word in words)
                            {
                                addCollection(word);
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }

            tbSearch.IsEnabled = btnGo.IsEnabled = true;
        }

        private void tbSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnGo.IsEnabled = (tbSearch.Text.Trim() != "");
        }
    }
}
