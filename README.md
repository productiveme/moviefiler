### Organizing a movie collection

Although iTunes does a very good job of organizing and tagging movie files, if you want to reference your movies by Imdb rating, genre or year of release without the need of a software package, MovieFiler will help.

### How it works

Since NTFS allows us to add hard or symbolic links, the idea was to create subfolders for the different meta tags and to place hard links to the original movie file in each. This creates the illusion of multiple copies of the same file in different subfolders, which btw perfectly fools a playstation through PS3 media server, but it doesn't waste all that space of actual copies.

### What it does

Usage 1: 
<pre class="lang:sh decode:true " >moviefiler.exe *.mp4 movies.csv</pre> 

The app iterates over the files and for each mp4 file it searches IMDB.com for the meta data based on the filename. It then moves the file to an "All" folder after which it creates folders and hardlinks in the following way:

   - "Year/2011" if release date was in 2011
   - "ABC/M" if the movie's title starts with "M"
   - "Genre/Comedy" if its genres include Comedy
   - "Rating/6.4" if the average rating was 6.4

The meta data is saved to a local CSV file (movies.csv in this case). Next time it will first match against the CSV file before trying IMDB.com again. 

### Undo wrong matches

Usage 2:
<pre class="lang:sh decode:true " >moviefiler.exe -u movies.csv</pre> 

After opening the local CSV file (movies.csv) and placing any character (e.g. "X") in the "Undo" column for the incorrectly matched movies, one can run the above command. This will remove these matches and move the files back to the original location.

### What about FAT and ExFAT?

I'm still trying to find a more elegant solution, but for now when it detects the drive is not NTFS, it will create a shortcut (.lnk) file in the subfolders that links to the "Movies/All" folder. If your "Movies" folder is on an external drive, unfortunately you'll have to ensure the same drive letter for the shortcuts to work.