/*************************************************************************************
 * Ratel Music Player
 * Loads all music in a folder entitled "Music" that is located in the same directory as
 * the executable.  Loads mp3 tags if they are compatible with mp3 ID3v1.  Plays music while
 * displaying an interactive trackbar.
 * Joe Silverstein, 5/9/11
 * ***************************************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using System.IO;
using WMPLib;


namespace RatelMusicPlayer
{
    public partial class Form1 : Form
    {
        private WindowsMediaPlayer player;
        string selectedSong = "";
        int elapsedTime = 1; // the elapsed time in a song (starts at 1 to make up for the delay in starting up the timer)
        int newElapsedTime = 0; //elapsed time + 1
        int trackBarValue;
        FileStream fileToWrite; // for saving music directory name
        FileStream fileToRead; // for reading in saved music directory name
        StreamReader fin;
        StreamWriter fout;

        public Form1()
        {
            InitializeComponent();
            player = new WindowsMediaPlayer(); // object needs to be created here for some bizarre reason (and not in Form1_Load or else nullreferenceexception will be thrown when setting URL)
            fileToRead = new FileStream(@".\folderName.txt", FileMode.Open);
            fin = new StreamReader(fileToRead);
            addMusic(fin.ReadLine()); // reads path of music directory and adds music from it
            fileToRead.Close(); 
            fileToWrite = new FileStream(@".\folderName.txt", FileMode.Create); // creates new file if does not exist
            fout = new StreamWriter(fileToWrite);
            songTimer.Enabled = true; // enables the song timer
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        // returns ID3v1 tag array.  ONLY works for mp3's using ID3v1 tags.
        // see http://en.wikipedia.org/wiki/ID3 for description of ID3 standard
        private string[] getTagArray(FileInfo file)
        {
            string[] myTagArray = new string[6]; // tag array to be returned
            string filePath = file.FullName; // gets full path

            using (FileStream fs = File.OpenRead(filePath))
            {
                if (fs.Length >= 128) // all ID3v1 mp3 tags are at least 128 bytes.  some are 227 bytes.
                {
                    MusicID3Tag tag = new MusicID3Tag();
                    /* ID3 tags are always placed at the end of the mp3 file. Extended ID3 tag data (for 227 byte tags) is 
                     * placed BEFORE the normal 128 bit tag.  Since we don't handle
                     * extended ID3 tags here, we want to start reading the tag data 128 bytes before the end of the stream. */
                    fs.Seek(-128, SeekOrigin.End); 
                    fs.Read(tag.TAGID, 0, tag.TAGID.Length); // reads 3 bytes of data (size of TAGID) into tag.TAGID field
                    fs.Read(tag.Title, 0, tag.Title.Length); // reads in 30 bytes of data (size of Title) into tag.Title field
                    fs.Read(tag.Artist, 0, tag.Artist.Length); // 30
                    fs.Read(tag.Album, 0, tag.Album.Length); // 30
                    fs.Read(tag.Year, 0, tag.Year.Length); // 4
                    fs.Read(tag.Comment, 0, tag.Comment.Length); // 30
                    //fs.Read(tag.Genre, 0, tag.Genre.Length); // 1
                    string theTAGID = Encoding.Default.GetString(tag.TAGID);

                    // the first 3 bytes of an mp3 tag say "TAG" to signify that it is ID3v1 and not some other tag standard.
                    if (theTAGID.Equals("TAG"))
                    {
                        /* converts the tags from pure bytes to strings using standard encoding.
                         * converts using ANSI standard, which is different from ASCII because each char is 8 bytes instead of 7 bytes.
                         * for ascii table showing standard encoding rules from bytes to chars, see http://en.wikipedia.org/wiki/ASCII#ASCII_printable_characters
                         * for more info on ANSI, see http://weblogs.asp.net/ahoffman/archive/2004/01/19/60094.aspx */
                        string Title = Encoding.Default.GetString(tag.Title);
                        string Artist = Encoding.Default.GetString(tag.Artist);
                        string Album = Encoding.Default.GetString(tag.Album);
                        string Year = Encoding.Default.GetString(tag.Year);
                        string Comment = Encoding.Default.GetString(tag.Comment);
                        //string Genre = Encoding.Default.GetString(tag.Genre);

                        myTagArray[0] = Title;
                        myTagArray[1] = Artist;
                        myTagArray[2] = Album;
                        myTagArray[3] = Year;
                        myTagArray[4] = Comment;
                        //myTagArray[5] = Genre;

                        /* After being encoded, the extra bytes that aren't valid chars get converted to strange
                         * chars to signify that the encoder doesn't know what it is.  
                         * We need to get rid of these "bad" chars.
                         * */
                        char c;
                        for (int j = 0; j < 5; j++)
                        {
                            for (int i = 0; i < myTagArray[j].Length; i++)
                            {
                                c = myTagArray[j][i];
                                if (!(Char.IsLetterOrDigit(c) || Char.IsPunctuation(c) || Char.IsWhiteSpace(c)))
                                {
                                    myTagArray[j] = myTagArray[j].Substring(0, i);
                                    break; // leave loop
                                }
                            }
                        }

                        // update myTagArray with new strings
                        for (int i = 0; i < 5; i++)
                            myTagArray[i] = myTagArray[i];
                    }
                    else
                        myTagArray[0] = "NOT ID3v1";
                }
            }

            myTagArray[5] = file.Name;

            return myTagArray;
        }

        // adds contents of music folder from given file strFolder
        private void addMusic(string strFolder)
        {
            if (strFolder == null)
                strFolder = @".\Music";
            DirectoryInfo di = new DirectoryInfo(@strFolder);
            FileInfo[] files = di.GetFiles();

            int i=0;
            // iterate though each of the files in the folder
            foreach (FileInfo file in files)
            {
                //ensures that only supported file types are added
                if (file.Name.EndsWith(".mp3") || file.Name.EndsWith(".wav") || file.Name.EndsWith(".wma")){
                    if (file.Name.EndsWith(".mp3")) {
                        string[] newRow = getTagArray(file); // retrieves tag array
                        if (newRow[0] == "NOT ID3v1") // add as if it were not an mp3
                        {
                            newRow[0] = file.Name;
                            for (int j = 1; i < newRow.Length; i++)
                                newRow[j] = "";
                            dataGridView1.Rows.Add(newRow);
                        }
                        else 
                            dataGridView1.Rows.Add(newRow);
                    }
                    else { // if it's not an mp3
                        string[] newRow = new string[6];
                        newRow[0] = file.Name;
                        newRow[1] = "";
                        newRow[2] = "";
                        newRow[3] = "";
                        newRow[4] = "";
                        newRow[5] = file.Name;
                        dataGridView1.Rows.Add(newRow);
                    }
                }
                i++;
            }
        }

        // plays the file specified by the URL
        private void PlayFile(String url)
        {
            player.MediaError +=
                new WMPLib._WMPOCXEvents_MediaErrorEventHandler(Player_MediaError); // if file type not supported
            player.URL = url;
            player.controls.play();
        }

        // when media type not supported
        private void Player_MediaError(object pMediaObject)
        {
            MessageBox.Show("Cannot play media file.");
            this.Close();
        }

        // plays the selected song
        private void playButton_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedCells.Count != 0) // make sure an item is selected
            {
                string lastSong = selectedSong;
                selectedSong = @".\Music\" + dataGridView1.SelectedRows[0].Cells[5].Value; // retrieve song name (full path)

                if (playButton.Text == "Play")
                {
                    //songTimer.Enabled = true;
                    PlayFile(selectedSong); // plays the file and handles errors
                    playButton.Text = "Pause";
                }
                else
                {
                    //songTimer.Enabled = false;
                    player.controls.pause();
                    playButton.Text = "Play";
                }
            }
        }

        int currentMediaDuration; // needed for casting to an int
        // runs every second when enabled
        private void songTimer_Tick(object sender, EventArgs e)
        {
            if (player.playState == WMPPlayState.wmppsPlaying)
            {
                newElapsedTime = (int)player.controls.currentPosition;
                if (newElapsedTime >= (int)player.currentMedia.duration)
                {
                    //songTimer.Enabled = false; //stop the timer
                    playButton.Text = "Play"; // reset play button
                }
                currentMediaDuration = (int)player.currentMedia.duration;
                elapsedTimeLabel.Text = newElapsedTime.ToString() + ".00 / " + currentMediaDuration.ToString() + ".00";
                trackBarValue = (int)(((double)(newElapsedTime) / player.currentMedia.duration) * trackBar1.Maximum);
                if (player.playState == WMPPlayState.wmppsPlaying)
                { // if a song is playing
                    if (trackBarValue < trackBar1.Maximum)
                        trackBar1.Value = trackBarValue;
                    else
                        playButton.Text = "Pause";
                }
                else
                    playButton.Text = "Play";
            }
        }

        // allows user to change his position in the song
        private void trackBar1_MouseUp(object sender, MouseEventArgs e)
        {
            player.controls.currentPosition = player.currentMedia.duration * ((double)trackBar1.Value / trackBar1.Maximum); // changes song position
            trackBar1.Value = (int)((player.controls.currentPosition / player.currentMedia.duration) * 100); // change track bar position
            elapsedTime = (int)player.controls.currentPosition; // change elapsed time to be displayed
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // brings up openFileDialog to choose song and plays it.
        // does not add the song to the datagridview
        // Compare to "SoundsWithWMPLib"
        private void openFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                    player.URL = openFileDialog1.FileName;
                    player.controls.play();
                    playButton.Text = "Pause";
            }
        }
    }

    //  standardized ID3 tag class (for mp3's)
    // for more info, visit http://en.wikipedia.org/wiki/ID3
    class MusicID3Tag
    {
        public byte[] TAGID = new byte[3];      //  3 bytes
        public byte[] Title = new byte[30];     //  30 bytes
        public byte[] Artist = new byte[30];    //  30 
        public byte[] Album = new byte[30];     //  30 
        public byte[] Year = new byte[4];       //  4 
        public byte[] Comment = new byte[30];   //  30 
        //public byte[] Genre = new byte[1];      //  1 byte (specifies number from list of genres)
        // for list of genres, see http://www.id3.org/id3v2.3.0#head-129376727ebe5309c1de1888987d070288d7c7e7
        // Note that the bytes add up to 128, which is the total size of the tag
        // most mp3's with ID3v2 now use 30 bytes for the genre and actually write it out, so need to be cautious with using genre byte.
    }
}