using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;



using Elasticsearch.Net;
using Nest;
using Microsoft.AspNetCore.Mvc;
using Nest.JsonNetSerializer;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Identity;
using Lexique.Models;
using System.IO;

namespace Lexique
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
       
        public void ConfigureServices(IServiceCollection services)
        {

            //services.AddControllersWithViews();
            //var pool = new SingleNodeConnectionPool(new Uri("http://localhost:9200"));
            //var settings = new ConnectionSettings(pool).DefaultIndex("lex");

            //var client = new ElasticClient(settings);

            //client.Indices.UpdateSettingsAsync("lex", s => s
            //.IndexSettings(i => i.Setting(UpdatableIndexSettings.MaxResultWindow, 100000)
            //.RefreshInterval("1s")));

            //services.AddSingleton(client);

            services.AddControllersWithViews();
            var pool = new SingleNodeConnectionPool(new Uri("http://localhost:9200"));
            services.AddHealthChecks();


            var connectionSettings =
            new ConnectionSettings(pool,
                sourceSerializer: (builtin, settings) => new JsonNetSerializer(builtin, settings,
                    () => new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All }
                ));
           
            var client = new ElasticClient(connectionSettings);

            var createIndexResponse = client.Indices
            .Create("lex", i => i.Map<Models.Lexique>(m => m.AutoMap()));

            var createIndexResponse2 = client.Indices
                .Create("attachments", i => i.Map<Models.Doc>(m => m.AutoMap()));

            //client.Index(new Models.Lexique { id = 2, libelle_fr = "jjjjjjj" },
            //i => i.Index("experience"));

            //var base64File = Convert.ToBase64String(File.ReadAllBytes(@"C:\Users\khali\Desktop\search_engine7\W12-5018.pdf"));

            //var indexResponse = client.Index(new Doc
            //{
            //    Id = 5,
            //    Path = @"C:\Users\khali\Desktop\search_engine7\W12-5018.pdf",
            //    Content = base64File
            //}, i => i
            //  .Pipeline("attachments")
            //  .Refresh(Refresh.WaitFor)
            //);

            client.Indices.Refresh();
            services.AddMvc();
            services.AddSingleton(client);

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();



            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
               

                endpoints.MapControllerRoute(
                    name: "default",
                    //pattern: "{controller=Home}/{action=Index}/{id?}");
                    pattern: "{controller=Home}/{action=Index}/{query?}/{selectedAnswer?}");
            });


        }
    }
}
