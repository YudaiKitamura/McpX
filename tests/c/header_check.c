/* mcpx.h が C11 で警告なくコンパイルでき、構造体のレイアウトが ABI（64bit）と一致することを確認する */
#include <stddef.h>
#include "mcpx.h"

_Static_assert(sizeof(void*) == 8, "64bit only");

_Static_assert(sizeof(mcpx_error) == 528, "mcpx_error size");
_Static_assert(offsetof(mcpx_error, status) == 4, "mcpx_error.status");
_Static_assert(offsetof(mcpx_error, end_code) == 8, "mcpx_error.end_code");
_Static_assert(offsetof(mcpx_error, socket_error) == 12, "mcpx_error.socket_error");
_Static_assert(offsetof(mcpx_error, message) == 16, "mcpx_error.message");

_Static_assert(sizeof(mcpx_connect_options) == 40, "mcpx_connect_options size");
_Static_assert(offsetof(mcpx_connect_options, host) == 8, "mcpx_connect_options.host");
_Static_assert(offsetof(mcpx_connect_options, password) == 16, "mcpx_connect_options.password");
_Static_assert(offsetof(mcpx_connect_options, timeout_ms) == 24, "mcpx_connect_options.timeout_ms");
_Static_assert(offsetof(mcpx_connect_options, is_ascii) == 28, "mcpx_connect_options.is_ascii");
_Static_assert(offsetof(mcpx_connect_options, use_multi_block) == 32, "mcpx_connect_options.use_multi_block");

_Static_assert(sizeof(mcpx_simulator_options) == 32, "mcpx_simulator_options size");
_Static_assert(offsetof(mcpx_simulator_options, host) == 16, "mcpx_simulator_options.host");
_Static_assert(offsetof(mcpx_simulator_options, timeout_ms) == 24, "mcpx_simulator_options.timeout_ms");
_Static_assert(offsetof(mcpx_simulator_options, frame) == 28, "mcpx_simulator_options.frame");

_Static_assert(sizeof(mcpx_item) == 32, "mcpx_item size");
_Static_assert(offsetof(mcpx_item, buffer) == 8, "mcpx_item.buffer");
_Static_assert(offsetof(mcpx_item, buffer_size) == 16, "mcpx_item.buffer_size");
_Static_assert(offsetof(mcpx_item, count) == 24, "mcpx_item.count");
_Static_assert(offsetof(mcpx_item, type) == 28, "mcpx_item.type");
_Static_assert(offsetof(mcpx_item, kind) == 30, "mcpx_item.kind");

int main(void) { return 0; }
