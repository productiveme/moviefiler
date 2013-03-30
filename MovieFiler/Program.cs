using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using FileHelpers;

namespace MovieFiler
{


    class Program
    {
        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        static extern bool CreateHardLink(
        string lpFileName,
        string lpExistingFileName,
        IntPtr lpSecurityAttributes
        );

        private static List<Movie> movieList = new List<Movie>();

        static void Main(string[] args)
        {
            if (args.Count() < 2)
            {
                Console.WriteLine("Either > MovieFiler filespec [outputfile]");
                Console.WriteLine("Or     > MovieFiler -u outputfile");
                Console.WriteLine("Options:");
                Console.WriteLine("\t  filespec: files to be compared with IMDB titles");
                Console.WriteLine("\toutputfile: output file for comma seperated values from IMDB");
                Console.WriteLine("\t        -u: Undo move files if Undo column in outputfile is not empty");
                Console.WriteLine();
                Console.WriteLine("e.g. MovieFiler *.mp4 output.csv");
                Console.WriteLine("  or MovieFiler -u output.csv");
                Console.ReadKey();
                return;
            }

            if (args[0].ToLower() == "-u")
            {
                UndoFiles(args[1]);
            }
            else
            {

                //GetAndMoveFiles(args[0], false, ref movieList);
                if (args.Count() >= 2)
                {
                    try
                    {
                        GetAndMoveFiles(args[0], false, ref movieList, args[1]);
                    }
                    catch
                    {
                        if (movieList.Count() > 0)
                        {
                            GenerateCSV(args[1], ref movieList);
                        }
                    }
                }
                else
                {
                    GetAndMoveFiles(args[0], false, ref movieList);
                }

            }
            Console.ReadKey();
        }

        private static void UndoFiles(string p)
        {
            movieList.Clear();
            FileHelperEngine<Movie> fh = new FileHelperEngine<Movie>();
            foreach (var m in fh.ReadFile(p))
            {
                if (!string.IsNullOrEmpty(m.Undo.Trim()))
                {
                    var f = new FileInfo(m.FileName);
                    foreach (var fn in Directory.EnumerateFiles(f.Directory.Parent.FullName, f.Name, SearchOption.AllDirectories))
                    {
                        var ff = new FileInfo(fn);
                        if (ff.Directory.Name == "All")
                        {
                            File.Move(ff.FullName, Path.Combine(ff.Directory.Parent.FullName, ff.Name));
                            Console.WriteLine(m.Title + " moved back.");
                        }
                        else
                        {
                            if (ff.Directory.FullName != f.Directory.Parent.FullName)
                            {
                                File.Delete(ff.FullName);
                            }
                            else
                            {
                                Console.WriteLine(m.Title + " already in right place.");
                            }
                        }
                    }

                }
                else
                {
                    movieList.Add(m);
                }
            }
            new FileInfo(p).Delete();
            GenerateCSV(p, ref movieList);
            Console.WriteLine("Done.");
        }

        private static void GenerateCSV(string p, ref List<Movie> movieList)
        {
            WriteDelimitedFile<Movie>(movieList, new FileInfo(p), ",");
            Console.WriteLine("Results stored in " + p);

        }

        ///
        /// Method to write a delimited file, given a generic enumerable.
        ///
        /// The type T
        /// an enumerable of type T
        /// The destination file info
        /// The delimiter ("," for example)
        public static void WriteDelimitedFile<T>(IEnumerable list, FileInfo saveFile, string delimiter)
        {
            if (list == null) return;
            StreamWriter sw;
            bool newfile;
            if (saveFile.Exists)
            {
                sw = saveFile.AppendText();
                newfile = false;
            }
            else
            {
                sw = saveFile.CreateText();
                newfile = true;
            }
            using (sw)
            {

                var props = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public);
                var headerNames = props.Select(x => x.Name);
                if (newfile) sw.WriteLine(string.Join(delimiter, headerNames.ToArray()));

                foreach (T item in list)
                {
                    T item1 = item; // to prevent access to modified closure
                    var values = props
                    .Select(x => x.GetValue(item1) ?? "") // the null coalescing operator, replace null with ""
                    .Select(x => x.ToString())
                    .Select(x => x.Contains(delimiter) || x.Contains("\n") ? "\"" + x + "\"" : x); // if a value contains the delimiter, surround with quotes
                    sw.WriteLine(string.Join(delimiter, values.ToArray()));
                }
                sw.Close();
            }
        }
        private static void GetAndMoveFiles(string p, bool recursive, ref List<Movie> movieList)
        {
            GetAndMoveFiles(p, recursive, ref movieList, null);
        }

        private static void GetAndMoveFiles(string p, bool recursive, ref List<Movie> movieList, string output)
        {
            string path, filespec;
            SplitPathAndFilespec(p, out path, out filespec);

            #region ensure folders exist
            string allPath = Path.Combine(path, "All");
            if (!Directory.Exists(allPath))
                Directory.CreateDirectory(allPath);
            string genrePath = Path.Combine(path, "Genre");
            if (!Directory.Exists(genrePath))
                Directory.CreateDirectory(genrePath);
            string abcPath = Path.Combine(path, "ABC");
            if (!Directory.Exists(abcPath))
                Directory.CreateDirectory(abcPath);
            string yearPath = Path.Combine(path, "Year");
            if (!Directory.Exists(yearPath))
                Directory.CreateDirectory(yearPath);
            string ratingPath = Path.Combine(path, "Rating");
            if (!Directory.Exists(ratingPath))
                Directory.CreateDirectory(ratingPath);
            #endregion

            var lst = (from o in Directory.EnumerateFiles(path, filespec, (recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                       select o);
            foreach (var fn in lst)
            {
                FileInfo f = new FileInfo(fn);
                
                var title = parseTitleFromFile(f);
                Console.Write(string.Format("Searching IMDB for {0} ... ", title));
                var m = new IMDb(title);
                if (m.status)
                {
                    Console.WriteLine(m.Id + ".");
                    DateTime rlsd;
                    double rating;

                    var movie = new Movie()
                    {
                        Title = m.Title,
                        ReleaseDate = DateTime.TryParse(m.ReleaseDate, out rlsd) ? rlsd : default(DateTime),
                        Rating = double.TryParse(m.Rating, out rating) ? rating : default(double),
                        ImdbID = m.Id,
                        Genre = string.Join("/", m.Genres.ToArray()),
                        OriginalFileName = f.FullName,
                        FileModifiedDate = f.LastWriteTime,
                        FileCreatedDate = f.CreationTime,
                        OriginalTitle = title
                    };

                    string fname = fn;
                    if (fn.Contains(@"\"))
                        fname = fn.Substring(fn.LastIndexOf(@"\") + 1);

                    //Move to All folder
                    try
                    {
                        File.Move(movie.OriginalFileName, Path.Combine(allPath, fname));
                    }
                    catch { //file in use and continue
                        continue;
                    }
                    movie.FileName = Path.Combine(allPath, fname);

                    movieList.Add(movie);

                    #region create hardlink for alphabet letter
                    AddLink(Path.Combine(abcPath, movie.Title.Substring(0, 1)), Path.Combine(allPath, fname));
                    #endregion

                    #region create hardlink for genre
                    if (!string.IsNullOrEmpty(movie.Genre))
                    {
                        foreach (var g in movie.Genre.Split('/'))
                        {
                            AddLink(Path.Combine(genrePath, g), Path.Combine(allPath, fname));
                        }
                    }
                    #endregion

                    #region create hardlink for year
                    if (movie.ReleaseDate != default(DateTime))
                    {
                        AddLink(Path.Combine(yearPath, movie.ReleaseDate.Value.Year.ToString()), Path.Combine(allPath, fname));
                    }
                    #endregion

                    #region create hardlink for rating
                    if (movie.Rating != default(double))
                    {
                        AddLink(Path.Combine(ratingPath, movie.Rating.ToString("#.#")), Path.Combine(allPath, fname));
                    }
                    #endregion


                }
                else
                {
                    Console.WriteLine("not found.");
                }


            }

            if (!string.IsNullOrEmpty(output))
            {
                GenerateCSV(output, ref movieList);
            }
        }

        private static void AddLink(string LinkPath, string FileFullName)
        {
            var f = new FileInfo(FileFullName);
            if (!Directory.Exists(LinkPath))
                Directory.CreateDirectory(LinkPath);

            if (new DriveInfo(FileFullName.Substring(0, 1)).DriveFormat == "NTFS")
            {
                CreateHardLink(Path.Combine(LinkPath, f.Name), FileFullName, IntPtr.Zero);
            }
            else
            {
                using (StreamWriter writer = new StreamWriter(LinkPath + "\\" + parseTitleFromFile(f) + ".url"))
                {
                    //string app = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    writer.WriteLine("[InternetShortcut]");
                    //System.Uri uri1 = new Uri(FileFullName);
                    //System.Uri uri2 = new Uri(LinkPath.TrimEnd("\\".ToCharArray()) + "\\");
                    //Uri relativeUri = uri2.MakeRelativeUri(uri1);
                    writer.WriteLine("URL=file:///" + FileFullName);
                    writer.WriteLine("IconIndex=0");
                    string icon = FileFullName.Replace('\\', '/');
                    writer.WriteLine("IconFile=" + icon);
                    writer.Flush();
                }
            }
        }

        private static void SplitPathAndFilespec(string p, out string path, out string filespec)
        {
            path = Environment.CurrentDirectory;
            filespec = p;
            if (p.Contains(@"\"))
            {
                path = p.Substring(0, p.LastIndexOf(@"\") + 1);
                filespec = p.Replace(path, "");
            }
        }

        private static string parseTitleFromFile(FileInfo f)
        {
            return f.Name.Substring(0, f.Name.LastIndexOf(f.Extension));
        }
    }
}
