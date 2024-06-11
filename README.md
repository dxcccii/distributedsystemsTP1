__________________________________________________________________________________________________________________________
                             8I                                                         
                             8I                                                         
                             8I                                               gg    gg  
                             8I                                               ""    ""  
                       ,gggg,8I     ,gg,   ,gg   ,gggg,    ,gggg,    ,gggg,   gg    gg  
                      dP"  "Y8I    d8""8b,dP"   dP"  "Yb  dP"  "Yb  dP"  "Yb  88    88  
                     i8'    ,8I   dP   ,88"    i8'       i8'       i8'        88    88  
                    ,d8,   ,d8b,,dP  ,dP"Y8,  ,d8,_    _,d8,_    _,d8,_    __,88,__,88,_
                   P"Y8888P"Y88"  dP"   "Y88P""Y8888PPP""Y8888PPP""Y8888PP8P""Y88P""Y8 
                                                                
              UTAD // COMPUTER SCIENCE // DISTRIBUTED SYSTEMS // BY "FYODOR" RIBEIRO // AL66766 

__________________________________________________________________________________________________________________________
                                                       
                             o                        |              o             
                                 _  _ _|_  ,_   __  __|         ___|_   __  _  _   
                             |  / |/ | |  /  | /  \/  |  |   | /   | | /  \/ |/ |  
                             |_/  |  |_/_/   |_|__/\_/|_/ \_/|_|___/_/_|__/  |  |_/
 __________________________________________________________________________________________________________________________

A Client/Server system that manages a client's tasks given a client ID. 
The interactions between the client and the server create a task assignment system where clients can request tasks from the 
server, mark tasks as completed, and end the communication when desired. The server is responsible for managing the 
client's requests, allocating tasks, and maintaining control over the status of the tasks.

The server can handle multiple clients at once by using different threads for each client. This system makes use of shared 
resources and employs mutexes to ensure that concurrent access to shared resources is properly managed, preventing race 
conditions and data corruption.
 __________________________________________________________________________________________________________________________
                                                       
                                  _  _                                                           
            o                    | || |       o                               |                  
                _  _    ,_|_ __, | || | __,_|_   __  _  _      __,   _  _   __|            ,  _  
            |  / |/ |  / \| /  | |/ |/ /  | | | /  \/ |/ |    /  |  / |/ | /  |    |   |  / \|/  
            |_/  |  |_/ \/|_|_/|_/__/__|_/|_/_/_|__/  |  |_/  \_/|_/  |  |_|_/|_/   \_/|_/ \/|__/
                                                                                                                                 
__________________________________________________________________________________________________________________________

Just clone the repository as normal, with visual studio. First run the server, then the client and to observe the changes
the server maskes to the files Servico_A.csv, Servico_B.csv, Servico_C.csv, or Servico_D.csv, please close the connection 
with the server first and then open the files in the debug folder through your file explorer.
__________________________________________________________________________________________________________________________                                                                 
                                   __   __   _  _  _|_  __,   __ _|_  ,  
                                  /    /  \_/ |/ |  |  /  |  /    |  / \_
                                  \___/\__/   |  |_/|_/\_/|_/\___/|_/ \/ 
___________________________________________________________________________________________________________________________                                     

contact dxcccii at https://dxcccii.neocities.org/ or by email (for now) at al66766@utad.eu
