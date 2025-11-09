For this assaignment I integrated an SQLite database.  
The application now creates and keeps items and orders, allowing me to have full data storage and retrieval between sessions.
Data is currently read and written via Entity Framework Core to an SQLite database. This allows for better practice os data integrity but its also a lot more complicated than keeping it locally. Or well that depends on how you access the database 'behind the scenes'. Lets say you have a program like SAP to easily control what is in your inventory then its a goood combination with this method. 
In the video you see that the first order have already been processed from a previous seesion, I then process the next order
I then close the program and open it again and it has kept the information, that I already have processed that order. 

