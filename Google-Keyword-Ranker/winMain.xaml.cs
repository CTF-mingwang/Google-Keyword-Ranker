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

        List<KeywordItem> keywordCollection = new List<KeywordItem>();

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
            public List<string> Metatag { get; set; }
        }

        void refreshCollection()
        {
            try
            {
                metas.Clear();
                tags.Clear();
                if (cbTitle.IsChecked == true)
                {
                    metatags.Add("title");
                }
                if (cbDescription.IsChecked == true)
                {
                    metatags.Add("description");
                }
                if (cbKeywords.IsChecked == true)
                {
                    metatags.Add("keywords");
                }
                if (cbH1.IsChecked == true)
                {
                    metatags.Add("h1");
                }
                if (cbH2.IsChecked == true)
                {
                    metatags.Add("h2");
                }
                if (cbH3.IsChecked == true)
                {
                    metatags.Add("h3");
                }
                refreshList();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                //throw;
            }
            
        }

        void addCollection(string str, string metatag)
        {
            if (str.Trim() == "")
            {
                return;
            }
            str = str.Trim();
            foreach (var keyword in keywordCollection)
            {
                if (keyword.Keyword == str)
                {
                    keyword.Frequency += 1;
                    Console.WriteLine(keyword.Frequency.ToString());
                    bool flag = true;
                    foreach (string metatagItem in keyword.Metatag)
                    {
                        if (metatagItem == metatag)
                        {
                            flag = false;
                            break;
                        }
                    }
                    if (flag)
                    {
                        keyword.Metatag.Add(metatag);
                    }
                    return;
                }
            }
            keywordCollection.Add(new KeywordItem { Keyword = str, Frequency = 1, Metatag = new List<string>(){metatag}});
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


        private List<string> metas = new List<string>();
        private List<string> tags = new List<string>();

        private List<string> metatags = new List<string>();

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

        void refreshList()
        {
            listKeyword.Items.Clear();
            bool flag;
            foreach (KeywordItem keyword in keywordCollection)
            {
                flag = false;
                List<string> newmetatags = new List<string>();
                foreach (string keywordmetatag in keyword.Metatag)
                {
                    foreach (string metatagitem in metatags)
                    {
                        if (keywordmetatag == metatagitem)
                        {
                            newmetatags.Add(keywordmetatag);
                        }
                    }
                }
                if (newmetatags.Count > 0)
                {
                    listKeyword.Items.Add(new { Keyword = keyword.Keyword, Frequency = keyword.Frequency, Metatag = String.Join(", ", keyword.Metatag) });
                }
                //MessageBox.Show(listKeyword.Items.Count.ToString());
            }
        }

        private void search ()
        {
            string query = tbSearch.Text;
            var results = Search(query);
            tags.Clear();
            tags.Add("title");
            metas.Add("//meta[@name='description']");
            metas.Add("//meta[@name='keywords']");
            tags.Add("h1");
            tags.Add("h2");
            tags.Add("h3");
            
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
                                        addCollection(word, (metaname == "//meta[@name='description']" ? "description" : "keywords"));
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
                                addCollection(word, tagname);
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }

            tbSearch.IsEnabled = btnGo.IsEnabled = true;
            refreshList();
        }

        private void tbSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnGo.IsEnabled = (tbSearch.Text.Trim() != "");
        }
        

        private void cbTitle_Checked(object sender, RoutedEventArgs e)
        {
            refreshCollection();
        }

        private void cbH2_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}
