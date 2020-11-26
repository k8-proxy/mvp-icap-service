#include <stdlib.h>
#include <string.h>
#include <time.h>

const char *szTemp = "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx";
const char *szHex = "0123456789abcdef-";

void generate_random_guid(unsigned char guid[40])
{
  srand (clock());

  int nLen = strlen (szTemp);

  for (int t=0; t<nLen+1; t++){
    int r = rand () % 16;
    char c = ' ';   

    switch (szTemp[t])
    {
      case 'x' : 
        c = szHex [r]; 
        break;
      case 'y' : 
        c = szHex [(r & 0x03) | 0x08]; 
        break;      
      case '-' : 
        c = '-'; 
        break;
      case '4' : 
        c = '4';
        break;
      default  : 
        break;
    }

    guid[t] = ( t < nLen ) ? c : 0x00;    
  }
}