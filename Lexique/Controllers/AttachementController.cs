using Elasticsearch.Net;
using Lexique.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lexique.Controllers
{
    public class AttachementController : Controller
    {

        private readonly ILogger<AttachementController> _logger;
        private readonly ElasticClient _client;
        private IHostingEnvironment Environment;


        public AttachementController(ILogger<AttachementController> logger, ElasticClient client, IHostingEnvironment _environment)
        {
            _logger = logger;
            _client = client;
            Environment = _environment;
        }


        public IActionResult Index(string query, string SelectedAnswer)
        {
            bool results_bool = false;
            List<Models.Doc> srch = new List<Models.Doc>();
            List<Models.Result> res = new List<Models.Result>();
            int id;
            String ar;
            String fr;
            List<string> arabic = new List<string>();
            List<string> francais = new List<string>();
            List<string> numeric = new List<string>();
            string correction_fr = "";
            string correction_ar = "";
            string correction = "";
            int count = 0;
            var watch_search = System.Diagnostics.Stopwatch.StartNew();

            if (query != null && SelectedAnswer == "avancee")
            {
                string[] words = query.Split(' ');
                Regex regex = new Regex("^[a-zA-Z0-9 àâäèéêëîïôœùûüÿçÀÂÄÈÉÊËÎÏÔŒÙÛÜŸÇ.,!?\\-_\'’\"]*$", RegexOptions.Compiled);

                foreach (var word in words)
                {

                    if (Regex.IsMatch(word, @"\p{IsArabic}"))
                    {
                        arabic.Add(word);
                    }
                    else
                    {
                        francais.Add(word);
                    }

                }


                String arb = String.Join(" ", arabic.ToArray());
                String frs = String.Join(" ", francais.ToArray());
                String num = String.Join(" ", numeric.ToArray());


                var results = _client.Search<Doc>(s => s
                .Index("attachments")
                   .Query(q => q
                        .Bool(b => b
                            .Must(mu => mu
                        .MatchPhrase(m => m
                            .Field(f => f.Attachment.Content.Suffix("fr"))
                            .Query(frs)), mu => mu
                        .MatchPhrase(m => m
                            .Field(f => f.Attachment.Content.Suffix("ar"))
                            .Query(arb)
                        )
                        //), mu => mu
                        //         .MultiMatch(m => m
                        //             .Fields(fs => fs
                        //                 .Field(p => p.Attachment.Content.Suffix("ar"))
                        //                 .Field(p => p.Attachment.Content.Suffix("fr"))
                        //             )
                        //             .Query(num)
                        //)
                    )
                        ))
                   .Highlight(h => h
                        .PreTags("<b class=\"mycolor\">")
                        .PostTags("</b>")
                        .FragmentSize(200)
                        .NumberOfFragments(0)
                        .Encoder(HighlighterEncoder.Html)
                        .Fields(f => f.Field("*"))
                   )
                   );

                if (results.Documents.Count > 0)
                {
                    results_bool = true;
                    foreach (Doc result in results.Documents) //cycle through your results
                    {
                        System.Diagnostics.Debug.WriteLine("content withput b : " + result.Attachment.Content);
                        System.Diagnostics.Debug.WriteLine("frs : " + frs);
                        count = result.Attachment.Content.Split(frs).Length - 1;
                        System.Diagnostics.Debug.WriteLine("count withput b : " + count);
                        foreach (var hit in results.Hits) // cycle through your hits to look for match
                        {
                            if (hit.Id == result.Id.ToString()) //you found the hit that matches your document
                            {
                                System.Diagnostics.Debug.WriteLine("Pour id : " + hit.Id + " score Lucene : " + hit.Score);
                                foreach (var highlightField in hit.Highlight)
                                {
                                    
                                    foreach (var highlight in highlightField.Value)
                                    {

                                        result.Content = highlight.ToString();
                                        count = result.Content.Split(query, (int)RegexOptions.IgnoreCase).Length - 1;
                                        System.Diagnostics.Debug.WriteLine("content : " + result.Content);
                                    }
                                    
                                }
                                res.Add(new Models.Result { Id = hit.Source.Id, Content = result.Content, Name = result.Name, Count = count, Path = result.Path });
                                //srch.Add(new Doc { Id = hit.Source.Id, Content = result.Content, Name = result.Name });
                            }
                        }
                    }
                }
                TempData["count"] = res.Count();

                ViewBag.data = res;
                //ViewBag.data = srch;
                TempData["simple"] = "avancee";
            }
            else if (query != null && SelectedAnswer == "simple")
            {

                string[] words = query.Split(' ');
                Regex regex = new Regex("^[a-zA-ZàâäèéêëîïôœùûüÿçÀÂÄÈÉÊËÎÏÔŒÙÛÜŸÇ]*$", RegexOptions.Compiled);

                foreach (var word in words)
                {
                    if (Regex.IsMatch(word, @"\p{IsArabic}"))
                    {
                        arabic.Add(word);
                    }
                    else
                    {
                        francais.Add(word);
                    }

                    //System.Diagnostics.Debug.WriteLine($"<{word}>");
                    //if (regex.IsMatch(word))
                    //{
                    //    System.Diagnostics.Debug.WriteLine("frech");
                    //    francais.Add(word);
                    //}
                    //else if (Regex.IsMatch(word, @"\p{IsArabic}"))
                    //{
                    //    System.Diagnostics.Debug.WriteLine("arabic");
                    //    arabic.Add(word);
                    //}
                    //else
                    //{
                    //    System.Diagnostics.Debug.WriteLine("num");
                    //    numeric.Add(word);
                    //}
                }


                String arb = String.Join(" ", arabic.ToArray());
                String frs = String.Join(" ", francais.ToArray());
                String num = String.Join(" ", numeric.ToArray());
                System.Diagnostics.Debug.WriteLine("araaaaaaab : " + arb);
                System.Diagnostics.Debug.WriteLine("frrrrrrrrr : " + frs);
                System.Diagnostics.Debug.WriteLine("nummmmmmmm :" + num);


                var results = _client.Search<Doc>(s => s
                .Index("attachments")
                   .Query(q => q
                        .Bool(b => b
                            .Should(mu => mu
                        .Match(m => m
                            .Field(f => f.Attachment.Content.Suffix("ar"))
                            .Query(arb)
                        ), mu => mu
                        .Match(m => m
                            .Field(f => f.Attachment.Content.Suffix("fr"))
                            .Boost(2)
                            .Query(frs)
                        ), mu => mu
                                 .MultiMatch(m => m
                                     .Fields(fs => fs
                                         .Field(p => p.Attachment.Content.Suffix("ar"))
                                         .Field(p => p.Attachment.Content.Suffix("fr"))
                                     )
                                     .Query(num)
                        )
                    )
                        ))
                   .Highlight(h => h
                        .PreTags("<b class=\"mycolor\">")
                        .PostTags("</b>")
                        .FragmentSize(200)
                        .NumberOfFragments(0)
                        .Encoder(HighlighterEncoder.Html)
                        .Fields(f => f.Field("*"))
                   )
                   );



                if (results.Documents.Count > 0)
                {
                    results_bool = true;
                    foreach (Doc result in results.Documents) //cycle through your results
                    {
                        foreach (var hit in results.Hits) // cycle through your hits to look for match
                        {
                            if (hit.Id == result.Id.ToString()) //you found the hit that matches your document
                            {

                                System.Diagnostics.Debug.WriteLine("Pour id : " + hit.Id + " score Lucene : " + hit.Score);
                                foreach (var highlightField in hit.Highlight)
                                {

                                    foreach (var highlight in highlightField.Value)
                                    {
                                        result.Content = highlight.ToString();
                                        count = result.Content.Split("<b class=\"mycolor\">").Length - 1;
                                        System.Diagnostics.Debug.WriteLine("Count : " + count);

                                    }

                                }
                                res.Add(new Models.Result { Id = hit.Source.Id, Content = result.Content, Name = result.Name, Count = count, Path = result.Path });
                                //srch.Add(new Doc { Id = hit.Source.Id, Content = result.Content, Name = result.Name });
                            }
                        }
                    }
                }
                //ViewBag.data = srch;
                TempData["count"] = res.Count();
                ViewBag.data = res;
                TempData["simple"] = "simple";
            }

            if (query != null && results_bool == false)
            {
                var results_fr = _client.Search<Models.Doc>(s => s
                .Index("attachments")
                .Suggest(su => su
                    .Phrase("my-phrase-fr-suggest", ph => ph
                    .Text(query)
                    .Field(p => p.Attachment.Content.Suffix("trigram"))
                    .Size(1)
                    .GramSize(3)
                    .DirectGenerator(d => d
                        .Field(p => p.Attachment.Content.Suffix("trigram"))
                        .SuggestMode(SuggestMode.Always)
                        )))
                      .Highlight(h => h
                           .PreTags("<b class=\"mycolor\">")
                           .PostTags("</b>")
                           .FragmentSize(200)
                           .NumberOfFragments(0)
                           .Encoder(HighlighterEncoder.Html)
                           .Fields(f => f.Field("*"))
                    ));


                var suggestions =
                from suggest in results_fr.Suggest["my-phrase-fr-suggest"]
                from option in suggest.Options
                select new string(option.Text.ToString());

                foreach (string r in suggestions.ToList())
                {
                    System.Diagnostics.Debug.WriteLine("Pour f &&: " + r);
                    correction_fr = r;
                }


                var results_ar = _client.Search<Models.Doc>(s => s
                .Index("attachments")
                .Suggest(su => su
                    .Phrase("my-phrase-ar-suggest", ph => ph
                    .Text(query)
                    .Field(p => p.Attachment.Content.Suffix("trigram"))
                    .Size(1)
                    .GramSize(3)
                    .DirectGenerator(d => d
                        .Field(p => p.Attachment.Content.Suffix("trigram"))
                        .SuggestMode(SuggestMode.Always)

                        ))
                        )

                      .Highlight(h => h
                           .PreTags("<b class=\"mycolor\">")
                           .PostTags("</b>")
                           .FragmentSize(200)
                           .NumberOfFragments(0)
                           .Encoder(HighlighterEncoder.Html)
                           .Fields(f => f.Field("*"))
                    )
                      );


                var suggestions_ar =
                from suggest in results_ar.Suggest["my-phrase-ar-suggest"]
                from option in suggest.Options
                select new string(option.Text.ToString());

                foreach (string r in suggestions_ar.ToList())
                {
                    System.Diagnostics.Debug.WriteLine("Pour a &&: " + r);
                    correction_ar = r;
                }

                System.Diagnostics.Debug.WriteLine("Pour f : " + correction_fr);
                System.Diagnostics.Debug.WriteLine("Pour a : " + correction_ar);

                string[] fr_words = correction_fr.Split(' ');
                foreach (var i in fr_words)
                {
                    if (!Regex.IsMatch(i, @"\p{IsArabic}"))
                    {
                        correction += i;
                    }
                    correction += " ";
                }

                string[] ar_words = correction_ar.Split(' ');
                foreach (var i in ar_words)
                {
                    if (Regex.IsMatch(i, @"\p{IsArabic}"))
                    {
                        correction += i;
                    }
                    correction += " ";
                }
                System.Diagnostics.Debug.WriteLine("Pour tous : " + correction);
                if (!string.IsNullOrWhiteSpace(correction))
                {
                    TempData["correction"] = correction;
                }

            }
            watch_search.Stop();
            double var = watch_search.ElapsedMilliseconds;
            double time_search = var / 1000;
            //System.Diagnostics.Debug.WriteLine("var : " + var +" time_search :"+ time_search);
            TempData["time_search"] = time_search.ToString();
            TempData["query"] = query;
            return View("Views/Home/Attachement.cshtml");
        }



  
        public IActionResult Delete(string name, int id, string query, string selectedAnswer)
        {
            System.Diagnostics.Debug.WriteLine("File : " + name);
            string path = Path.Combine(this.Environment.WebRootPath, "Uploads");
            if (System.IO.File.Exists(System.IO.Path.Combine(path, name)))
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                System.Diagnostics.Debug.WriteLine("File : " + name);
                

                var response = _client.DeleteByQuery<Doc>(q => q
                .Index("attachments")
                    .Query(rq => rq
                        .Match(m => m
                        .Field(f => f.Id)
                        .Query(id.ToString())
                    )));

                // If file found, delete it    
                System.IO.File.Delete(System.IO.Path.Combine(path, name));
                TempData["message"] = "fichier supprime";

                watch.Stop();
                double var = watch.ElapsedMilliseconds;
                double time_delete = var / 1000;
                TempData["message"] = "Document " + id.ToString() + " est supprime in " + time_delete.ToString() + " s";

                
                //System.Threading.Thread.Sleep(1000);
            }
            else
            {
                TempData["message"] = "fichier non supprime";
            }
            TempData["query"] = query;
            TempData["selectedAnswer"] = selectedAnswer;
            return Redirect(Url.Action("Index", "Attachement", new { query = query, selectedAnswer = selectedAnswer }));
        }

        public IActionResult Upload(List<IFormFile> postedFiles, string query, string selectedAnswer)
        {
            int id = 0, i = 0;
            bool isEmpty = !postedFiles.Any();
            if (!isEmpty)
            {
                string wwwPath = this.Environment.WebRootPath;
                string contentPath = this.Environment.ContentRootPath;

                string path = Path.Combine(this.Environment.WebRootPath, "Uploads");
                //string path = Path.Combine(@"C:\Users\khali\Desktop\search_engine7", "Uploads");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                List<string> uploadedFiles = new List<string>();

                var search = _client.Search<Doc>(s => s
                .Index("attachments")
                    .Size(1)
                    .Sort(ss => ss
                        .Descending(p => p.Id)
                    )
                    .Query(q => q
                        .MatchAll(m => m)
                        ));

                if (search.Documents.Count > 0)
                {
                    foreach (Doc result in search.Documents) //cycle through your results
                    {
                        foreach (var hit in search.Hits) // cycle through your hits to look for match
                        {
                            if (hit.Id == result.Id.ToString()) //you found the hit that matches your document
                            {
                                id = Convert.ToInt16(hit.Id);
                            }
                        }
                    }
                }

                foreach (IFormFile postedFile in postedFiles)
                {
                    i += 1;
                    string fileName = Path.GetFileName(postedFile.FileName);

                    using (FileStream stream = new FileStream(Path.Combine(path, fileName), FileMode.Create))
                    {
                        postedFile.CopyTo(stream);
                        uploadedFiles.Add(fileName);
                        TempData["message"] = string.Format("{0} uploaded.", fileName);
                    }
                    string base64File = Convert.ToBase64String(System.IO.File.ReadAllBytes(Path.Combine(path, fileName)));

                    var indexResponse = _client.Index(new Doc
                    {
                        Id = id + i,
                        Name = fileName,
                        Path = wwwPath,
                        Content = base64File
                    }, i => i
                      .Pipeline("attachments")
                      .Index("attachments")
                );
                }
            }
            return Redirect(Url.Action("Index", "Attachement", new { query = query, selectedAnswer = selectedAnswer }));
        }
    }
}
