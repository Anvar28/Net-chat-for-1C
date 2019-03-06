using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace client
{
    class TFlowDocument
    {
        public FlowDocument _fd;

        public TFlowDocument(FlowDocument fd)
        {
            _fd = fd;
        }

        public Paragraph CreateParagraph(string[] mStr)
        {
            if (mStr.Count() == 0)
                return null;

            Paragraph paragraph = new Paragraph();
            paragraph.Inlines.Add(new Bold(new Run(DateTime.Now.ToString() + "\r\n")));

            foreach (string item in mStr)
            {
                paragraph.Inlines.Add(new Run(item + "\r\n"));
            }

            return paragraph;
        }

        public void Write(Paragraph pr)
        {
            if (pr != null)
            {
                BlockCollection Blocks = _fd.Blocks;
                if (Blocks.Count == 0)
                    Blocks.Add(pr);
                else
                    Blocks.InsertBefore(Blocks.FirstBlock, pr);
            }
        }

        public void Write(string[] str)
        {
            Write(CreateParagraph(str));
        }

        public void Write(string str)
        {
            Write(new string[] { str });
        }
    }
}
