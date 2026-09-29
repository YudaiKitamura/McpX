/*
 * AOT publish したライブラリを dlopen して呼び出すスモークテスト（Linux / macOS）。
 *   cc -std=c11 -Wall -Werror -I../../McpXInterop/include smoke.c -o smoke -ldl
 *   ./smoke <ライブラリのパス>
 * 環境変数 MCPX_PLC_HOST / MCPX_PLC_PORT を指定すると、実機に接続して D0〜D9 を読み込む。
 */
#include <dlfcn.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include "mcpx.h"

#define LOAD(name) name##_fn name = (name##_fn)dlsym(lib, #name); if (!name) { fprintf(stderr, "missing %s\n", #name); return 1; }

typedef uint32_t (*mcpx_abi_version_fn)(void);
typedef const char* (*mcpx_version_fn)(void);
typedef uint32_t (*mcpx_struct_size_fn)(uint32_t);
typedef void (*mcpx_connect_options_init_fn)(mcpx_connect_options*);
typedef mcpx_status_t (*mcpx_connect_fn)(const mcpx_connect_options*, mcpx_client_t*, mcpx_error*);
typedef mcpx_status_t (*mcpx_close_fn)(mcpx_client_t, mcpx_error*);
typedef mcpx_status_t (*mcpx_batch_read_fn)(mcpx_client_t, uint8_t, const char*, uint8_t, uint32_t, void*, size_t, mcpx_error*);

#define CHECK(cond) do { if (!(cond)) { fprintf(stderr, "FAILED: %s (line %d)\n", #cond, __LINE__); return 1; } } while (0)

int main(int argc, char** argv)
{
    if (argc < 2) { fprintf(stderr, "usage: smoke <library>\n"); return 2; }

    void* lib = dlopen(argv[1], RTLD_NOW);
    if (!lib) { fprintf(stderr, "dlopen: %s\n", dlerror()); return 1; }

    LOAD(mcpx_abi_version) LOAD(mcpx_version) LOAD(mcpx_struct_size) LOAD(mcpx_connect_options_init)
    LOAD(mcpx_connect) LOAD(mcpx_close) LOAD(mcpx_batch_read)

    CHECK(mcpx_abi_version() >> 16 == MCPX_ABI_VERSION >> 16);
    CHECK(mcpx_struct_size(MCPX_STRUCT_ERROR) == sizeof(mcpx_error));
    CHECK(mcpx_struct_size(MCPX_STRUCT_CONNECT_OPTIONS) == sizeof(mcpx_connect_options));
    CHECK(mcpx_struct_size(MCPX_STRUCT_SIMULATOR_OPTIONS) == sizeof(mcpx_simulator_options));
    printf("mcpx %s (abi %u.%u)\n", mcpx_version(), mcpx_abi_version() >> 16, mcpx_abi_version() & 0xFFFF);

    mcpx_error err = { .struct_size = sizeof(mcpx_error) };

    /* 引数の検証（例外がネイティブ側へ漏れずに状態コードで返ること） */
    mcpx_client_t client = 0;
    CHECK(mcpx_connect(NULL, &client, &err) == MCPX_E_INVALID_ARGUMENT);
    CHECK(err.status == MCPX_E_INVALID_ARGUMENT && strlen(err.message) > 0);
    CHECK(mcpx_close(0, &err) == MCPX_OK);

    int16_t values[10];
    CHECK(mcpx_batch_read(987654321, MCPX_PREFIX_D, "0", MCPX_TYPE_I16, 10, values, sizeof(values), &err) == MCPX_E_INVALID_HANDLE);

    /* 実機（任意） */
    const char* host = getenv("MCPX_PLC_HOST");
    if (host != NULL)
    {
        mcpx_connect_options options;
        mcpx_connect_options_init(&options);
        options.host = host;
        options.port = getenv("MCPX_PLC_PORT") ? atoi(getenv("MCPX_PLC_PORT")) : 10000;

        mcpx_status_t status = mcpx_connect(&options, &client, &err);
        if (status != MCPX_OK) { fprintf(stderr, "connect: %d %s\n", status, err.message); return 1; }

        status = mcpx_batch_read(client, MCPX_PREFIX_D, "0", MCPX_TYPE_I16, 10, values, sizeof(values), &err);
        if (status != MCPX_OK) { fprintf(stderr, "batch_read: %d %s\n", status, err.message); return 1; }

        printf("D0-D9:");
        for (int i = 0; i < 10; i++) printf(" %d", values[i]);
        printf("\n");

        CHECK(mcpx_close(client, &err) == MCPX_OK);
    }

    printf("smoke: OK\n");
    return 0;
}
