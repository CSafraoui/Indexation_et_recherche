using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lexique.Models
{
    public class Doc
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string Content { get; set; }
        public Attachment Attachment { get; set; }
    }
}
