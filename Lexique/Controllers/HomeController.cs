using Elasticsearch.Net;
using Lexique.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Nest;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace Lexique.Controllers
{
    public class HomeController : Controller
    {
        // connexion base de donnees
        SqlConnection connection = new SqlConnection(@"");

        private readonly ILogger<HomeController> _logger;

        private readonly ElasticClient _client;
        List<Models.Lexique> srch = new List<Models.Lexique>();
        int id;
        



        public HomeController(ILogger<HomeController> logger, ElasticClient client)
        {
            _logger = logger;
            _client = client;
        }

        //[Route("Home/Index/{query?}/{selectedAnswer?}")]
        [Route("")]
        [Route("Home")]
        [Route("Home/Index")]
        [Route("Home/Index/{query?}/{selectedAnswer?}")]
        public IActionResult Index(string query, string selectedAnswer)
        {
            System.Diagnostics.Debug.WriteLine("It's index query : " + query + " selectedAnswer " + selectedAnswer);
            bool results_bool = false;
            List<Models.Lexique> srch = new List<Models.Lexique>();
            int id;
            String ar;
            String fr;
            List<string> arabic = new List<string>();
            List<string> francais = new List<string>();
            List<string> numeric = new List<string>();
            string correction_fr = "";
            string correction_ar = "";
            string correction = "";
            var watch_search = System.Diagnostics.Stopwatch.StartNew();
            if (query != null && selectedAnswer == "simple")
            {
                string[] words = query.Split(' ');
                Regex regex = new Regex("^[a-zA-ZàâäèéêëîïôœùûüÿçÀÂÄÈÉÊËÎÏÔŒÙÛÜŸÇ]*$", RegexOptions.Compiled);

                foreach (var word in words)
                {
                    System.Diagnostics.Debug.WriteLine($"<{word}>");

                    if (Regex.IsMatch(word, @"\p{IsArabic}"))
                    {
                        arabic.Add(word);
                    }
                    else
                    {
                        francais.Add(word);
                    }

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


                var results = _client.Search<Models.Lexique>(s => s
                .Index("lex")
                   .Query(q => q
                        .Bool(b => b
                            .Must(mu => mu
                        .MatchPhrase(m => m
                            .Field(f => f.libelle_ar)
                            .Query(arb)
                        ), mu => mu
                        .MatchPhrase(m => m
                            .Field(f => f.libelle_fr)
                            .Query(frs)
                        ), mu => mu
                                 .MultiMatch(m => m
                                     .Fields(fs => fs
                                         .Field(p => p.libelle_ar)
                                         .Field(p => p.libelle_fr)
                                     )
                                     .Query(num)
                        )
                    )
                        ))
                   .Highlight(h => h
                        .PreTags("<b class=\"mycolor\">")
                        .PostTags("</b>")
                        .FragmentSize(200)
                        .Encoder(HighlighterEncoder.Html)
                        .Fields(f => f.Field("*"))
                   )
                   );

                if (results.Documents.Count > 0)
                {
                    results_bool = true;
                    foreach (Models.Lexique result in results.Documents) //cycle through your results
                    {
                        foreach (var hit in results.Hits) // cycle through your hits to look for match
                        {
                            if (hit.Id == result.id.ToString()) //you found the hit that matches your document
                            {
                                System.Diagnostics.Debug.WriteLine("Pour id : " + hit.Id + " score Lucene : " + hit.Score);
                                foreach (var highlightField in hit.Highlight)
                                {
                                    if (highlightField.Key == "libelle_fr")
                                    {
                                        foreach (var highlight in highlightField.Value)
                                        {
                                            result.libelle_fr = highlight.ToString();
                                        }
                                    }
                                    else if (highlightField.Key == "libelle_ar")
                                    {
                                        foreach (var highlight in highlightField.Value)
                                        {
                                            result.libelle_ar = highlight.ToString();
                                        }
                                    }
                                }
                                srch.Add(new Models.Lexique { id = hit.Source.id, libelle_ar = result.libelle_ar, libelle_fr = result.libelle_fr });
                            }
                        }
                    }
                }
                TempData["count"] = srch.Count();
                ViewBag.data = srch;
                TempData["simple"] = "simple";
            }
            else if (query != null && selectedAnswer == "avancee")
            {
                string[] words = query.Split(' ');
                Regex regex = new Regex("^[a-zA-ZàâäèéêëîïôœùûüÿçÀÂÄÈÉÊËÎÏÔŒÙÛÜŸÇ]*$", RegexOptions.Compiled);

                foreach (var word in words)
                {
                    System.Diagnostics.Debug.WriteLine($"<{word}>");

                    if (Regex.IsMatch(word, @"\p{IsArabic}"))
                    {
                        arabic.Add(word);
                    }
                    else
                    {
                        francais.Add(word);
                    }

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


                var results = _client.Search<Models.Lexique>(s => s
                .Index("lex")
                .Size(1000)
                   .Query(q => q
                        .Bool(b => b
                            .Should(mu => mu
                        .Match(m => m
                            .Field(f => f.libelle_ar)
                            .Query(arb)
                        ), mu => mu
                        .Match(m => m
                            .Field(f => f.libelle_fr)
                            .Boost(2)
                            .Query(frs)
                        )//, mu => mu
                        //         .MultiMatch(m => m
                        //             .Fields(fs => fs
                        //                 .Field(p => p.libelle_ar)
                        //                 .Field(p => p.libelle_fr)
                        //             )
                        //             .Query(num)
                        //)
                    )
                        ))
                   .Highlight(h => h
                        .PreTags("<b class=\"mycolor\">")
                        .PostTags("</b>")
                        .FragmentSize(200)
                        .Encoder(HighlighterEncoder.Html)
                        .Fields(f => f.Field("*"))
                   )
                   );

                if (results.Documents.Count > 0)
                {
                    results_bool = true;
                    foreach (Models.Lexique result in results.Documents) //cycle through your results
                    {
                        foreach (var hit in results.Hits) // cycle through your hits to look for match
                        {
                            if (hit.Id == result.id.ToString()) //you found the hit that matches your document
                            {
                                System.Diagnostics.Debug.WriteLine("Pour id : " + hit.Id + " score Lucene : " + hit.Score);
                                foreach (var highlightField in hit.Highlight)
                                {
                                    if (highlightField.Key == "libelle_fr")
                                    {
                                        foreach (var highlight in highlightField.Value)
                                        {
                                            result.libelle_fr = highlight.ToString();
                                        }
                                    }
                                    else if (highlightField.Key == "libelle_ar")
                                    {
                                        foreach (var highlight in highlightField.Value)
                                        {
                                            result.libelle_ar = highlight.ToString();
                                        }
                                    }
                                }
                                srch.Add(new Models.Lexique { id = hit.Source.id, libelle_ar = result.libelle_ar, libelle_fr = result.libelle_fr });
                            }
                        }
                    }
                }
                TempData["count"] = srch.Count();
                ViewBag.data = srch;
                TempData["simple"] = "avancee";
            }

            if (query != null && results_bool == false)
            {
                try
                {
                    var results_fr = _client.Search<Models.Lexique>(s => s
                 .Index("lex")
                .Suggest(su => su
                    .Phrase("my-phrase-fr-suggest", ph => ph
                    .Text(query)
                    .Field(p => p.libelle_fr.Suffix("bigram"))
                    .Size(1)
                    .GramSize(3)
                    .DirectGenerator(d => d
                        .Field(p => p.libelle_fr.Suffix("bigram"))
                        .SuggestMode(Elasticsearch.Net.SuggestMode.Always)
                        )))
                      .Highlight(h => h
                           .PreTags("<b class=\"mycolor\">")
                           .PostTags("</b>")
                           .FragmentSize(200)
                           .Encoder(HighlighterEncoder.Html)
                           .Fields(f => f.Field("*"))
                    ));


                    var suggestions =
                    from suggest in results_fr.Suggest["my-phrase-fr-suggest"]
                    from option in suggest.Options
                    select new string(option.Text.ToString());

                    foreach (string r in suggestions.ToList())
                    {
                        correction_fr = r;
                    }


                    var results_ar = _client.Search<Models.Lexique>(s => s
                     .Index("lex")
                    .Suggest(su => su
                        .Phrase("my-phrase-ar-suggest", ph => ph
                        .Text(query)
                        .Field(p => p.libelle_ar.Suffix("bigram"))
                        .Size(1)
                        .GramSize(3)
                        .DirectGenerator(d => d
                            .Field(p => p.libelle_ar.Suffix("bigram"))
                            .SuggestMode(Elasticsearch.Net.SuggestMode.Always)
                            ))
                            )
                          .Highlight(h => h
                               .PreTags("<b class=\"mycolor\">")
                               .PostTags("</b>")
                               .FragmentSize(200)
                               .Encoder(HighlighterEncoder.Html)
                               .Fields(f => f.Field("*"))
                        )
                          );
                    System.Diagnostics.Debug.WriteLine("Suggest : " + results_ar);
                    var suggestions_ar =
                    from suggest in results_ar.Suggest["my-phrase-ar-suggest"]
                    from option in suggest.Options
                    select new string(option.Text.ToString());

                    foreach (string r in suggestions_ar.ToList())
                    {
                        correction_ar = r;
                    }

                    if (correction_ar != null && string.IsNullOrEmpty(correction_ar) == false && correction_fr != null && string.IsNullOrEmpty(correction_fr) == false)
                    {
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
                    }
                    else if (correction_ar != null && string.IsNullOrEmpty(correction_ar) == false)
                    {
                        correction = correction_ar;
                    }
                    else if (correction_fr != null && string.IsNullOrEmpty(correction_fr) == false)
                    {
                        correction = correction_fr;
                    }
                    System.Diagnostics.Debug.WriteLine("Pour tous : " + correction);
                    if (!string.IsNullOrWhiteSpace(correction))
                    {
                        TempData["correction"] = correction;
                    }
                }
                catch
                {

                }
            }
            
            watch_search.Stop();
            double var = watch_search.ElapsedMilliseconds;
            double time_search = var / 1000;
            
            TempData["time_search"] = time_search.ToString();
            TempData["query"] = query;
            
            return View();

        }

        public IActionResult Delete(string id, string query, string selectedAnswer)
        {
            if(id != null)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = _client.DeleteByQuery<Models.Lexique>(q => q
                 .Index("lex")
                    .Query(rq => rq
                        .Match(m => m
                        .Field(f => f.id)
                        .Query(id))
                    ));

                connection.Open();
                SqlCommand cmd = connection.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "delete from [Lexique] where Id = '" + id + "'";
                cmd.ExecuteNonQuery();
                connection.Close();

                watch.Stop();
                double var = watch.ElapsedMilliseconds;
                double time_delete = var / 1000;
                TempData["message"] = "Document " + id.ToString() + " est supprime en " + time_delete.ToString() + " s";

                TempData["query"] = query;
                TempData["selectedAnswer"] = selectedAnswer;
                System.Threading.Thread.Sleep(1000);

            }
            
            return Redirect(Url.Action("Index", "Home", new { query = query, selectedAnswer = selectedAnswer }));
            
        }

        public IActionResult Insert(string libelle_ar, string libelle_fr, string selectedAnswer, string query)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            System.Diagnostics.Debug.WriteLine("It's insert query : "+ query + " selectedAnswer "+ selectedAnswer);
            if (libelle_ar != null)
            {
                Regex regex_fr = new Regex("^[a-zA-Z0-9 àâäèéêëîïôœùûüÿçÀÂÄÈÉÊËÎÏÔŒÙÛÜŸÇ.,!?\\-_\'’\"]*$", RegexOptions.Compiled);

                if (Regex.IsMatch(libelle_ar, @"\p{IsArabic}") && regex_fr.IsMatch(libelle_fr))
                {
                    
                    connection.Open();
                    SqlCommand cmd = new SqlCommand("INSERT INTO [dbo].[Lexique] (libelle_ar, libelle_fr) VALUES (@libelle_ar, @libelle_fr)", connection);
                  
                    cmd.Parameters.AddWithValue("@libelle_ar", libelle_ar);
                    cmd.Parameters.AddWithValue("@libelle_fr", libelle_fr);
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "SELECT MAX(Id) FROM Lexique";
                    int id = Convert.ToInt32(cmd.ExecuteScalar());
                    var obj = new Models.Lexique
                    {
                        id = id,
                        libelle_ar = libelle_ar,
                        libelle_fr = libelle_fr
                    };

                    var response = _client.Index(obj, i => i
                       .Index("lex")
                      .Id(obj.id.ToString())
                      );
                    connection.Close();
                    System.Diagnostics.Debug.WriteLine(response.IsValid);
                    System.Diagnostics.Debug.WriteLine("////");
                    watch.Stop();
                    double var = watch.ElapsedMilliseconds;
                    double time_insert = var / 1000;

                    TempData["message"] = "Document insere en "+ time_insert.ToString() + " s";

                }
                else if (!Regex.IsMatch(libelle_ar, @"\p{IsArabic}"))
                {
                    TempData["message"] = "Invalid libelle_Ar";
                }
                else if (!regex_fr.IsMatch(libelle_fr))
                {
                    TempData["message"] = "Invalid libelle_Fr";
                }
            }
           
            System.Threading.Thread.Sleep(1000);
            return Redirect(Url.Action("Index", "Home", new { query = query, selectedAnswer = selectedAnswer }));
            
        }

        public JsonResult GetLexiques(string term)
        {
            List<string> lexiques = new List<string>() { };

            var results5 = _client.Search<Models.Lexique>(s => s
             .Index("lex")
                   .Query(q => q
                        .Prefix(c => c
                        .Field(p => p.libelle_fr)
                        .Value(term)
                        .Rewrite(MultiTermQueryRewrite.TopTerms(10))
                        )
                        )
                   .Highlight(h => h
                        .PreTags("<b class=\"mycolor\">")
                        .PostTags("</b>")
                        .FragmentSize(200)
                        .Encoder(HighlighterEncoder.Html)
                        .Fields(f => f.Field("*"))
                   )
                   );


            if (results5.Documents.Count > 0)
            {
                foreach (Models.Lexique result in results5.Documents) //cycle through your results
                {
                    lexiques.Add(result.libelle_fr.ToString());
                }
            }
            return Json(lexiques);
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
