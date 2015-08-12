using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wndprocTest
{
    class tree
    {

        public class node
        {
            public UInt32 instID;
            List<node> children = new List<node>();
            public int size;
            public node parent;

            public node(UInt32 instID)
            {
                this.instID = instID;
                size = 0;
                parent = null;
            }

            public UInt32 getInst()
            {
                return this.instID;
            }

            public int setParent(node parent)
            {
                try
                {
                    this.parent = parent;
                    return 0;
                }
                catch
                {
                    return 1;
                }
            }

            public int add(UInt32 childIst)
            {
                node child = new node(childIst);

                try
                {
                    this.children.Add(child);
                    size++;
                    return 0;
                }
                catch
                {
                    return 1;
                }
            }

            public List<node> getAll(ref int si)
            {
                si = this.size;
                return children;
            }

        }

        public static void disp(node root)
        {
            Console.WriteLine(root.getInst());
            int size = 0;
            List<node> children = new List<node>();
            children = root.getAll(ref size);

            for (int i = 0; i < size; i++)
            {
                disp(children[i]);
            }
        }

        public static void myMain(String[] args)
        {
            node test1 = new node(123123123);
            test1.add(123123);
            test1.add(32342342);
            test1.add(123123);

            List<node> child = new List<node>();
            int size = 0;
            child = test1.getAll(ref size);
            for (int i = 0; i < size; i++)
            {
                UInt32 id = (UInt32)i;
                Console.WriteLine(child[i].add(id));

            }
            disp(test1);
            Console.WriteLine("<Press Enter to exit>");
            string end = Console.ReadLine();
        }
    }
}
