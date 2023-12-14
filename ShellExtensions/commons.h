#pragma once

#define IDM_COMPUTE_AUTO            0
#define IDM_COMPUTE_XXH32           1
#define IDM_COMPUTE_XXH64           2
#define IDM_COMPUTE_XXH3_64         3
#define IDM_COMPUTE_XXH3_128        4
#define IDM_COMPUTE_SM3             5
#define IDM_COMPUTE_MD5             6
#define IDM_COMPUTE_CRC32           7
#define IDM_COMPUTE_CRC64           8
#define IDM_COMPUTE_QUICKXOR        9
#define IDM_COMPUTE_WHIRLPOOL       10
#define IDM_COMPUTE_SHA1            11
#define IDM_COMPUTE_SHA224          12
#define IDM_COMPUTE_SHA256          13
#define IDM_COMPUTE_SHA384          14
#define IDM_COMPUTE_SHA512          15
#define IDM_COMPUTE_SHA3_224        16
#define IDM_COMPUTE_SHA3_256        17
#define IDM_COMPUTE_SHA3_384        18
#define IDM_COMPUTE_SHA3_512        19
#define IDM_COMPUTE_BLAKE2B         20
#define IDM_COMPUTE_BLAKE2BP        21
#define IDM_COMPUTE_BLAKE2S         22
#define IDM_COMPUTE_BLAKE2SP        23
#define IDM_COMPUTE_BLAKE3          24
#define IDM_COMPUTE_STREEBOG_256    25
#define IDM_COMPUTE_PARENT          26

#define HC_EXECUTABLE               L"HashCalculator.exe"
#define HCEXE_REGPATH               L"Software\\Microsoft\\Windows\\CurrentVersion\\App Paths"
#define MAX_CMD_CHARS               32767

BOOL GetHashCalculatorPath(LPWSTR* buffer, LPDWORD bufsize);
VOID ShowMessageType(HMODULE hModule, UINT titleID, UINT messageID, UINT uType);
