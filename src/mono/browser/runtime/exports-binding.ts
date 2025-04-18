// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";

import { mono_wasm_debugger_log, mono_wasm_add_dbg_command_received, mono_wasm_set_entrypoint_breakpoint, mono_wasm_fire_debugger_agent_message_with_data, mono_wasm_fire_debugger_agent_message_with_data_to_pause } from "./debug";
import { mono_wasm_release_cs_owned_object } from "./gc-handles";
import { mono_wasm_bind_js_import_ST, mono_wasm_invoke_js_function, mono_wasm_invoke_jsimport_MT, mono_wasm_invoke_jsimport_ST } from "./invoke-js";
import { mono_interp_tier_prepare_jiterpreter, mono_wasm_free_method_data } from "./jiterpreter";
import { mono_interp_jit_wasm_entry_trampoline, mono_interp_record_interp_entry } from "./jiterpreter-interp-entry";
import { mono_interp_jit_wasm_jit_call_trampoline, mono_interp_invoke_wasm_jit_call_trampoline, mono_interp_flush_jitcall_queue } from "./jiterpreter-jit-call";
import { mono_wasm_resolve_or_reject_promise } from "./marshal-to-js";
import { mono_wasm_schedule_timer, schedule_background_exec } from "./scheduling";
import { mono_wasm_asm_loaded, mono_wasm_process_current_pid } from "./startup";
import { mono_log_warn, mono_wasm_console_clear, mono_wasm_trace_logger } from "./logging";
import { mono_wasm_browser_entropy } from "./crypto";
import { mono_wasm_cancel_promise } from "./cancelable-promise";

import {
    mono_wasm_start_deputy_thread_async,
    mono_wasm_pthread_on_pthread_attached, mono_wasm_pthread_on_pthread_unregistered,
    mono_wasm_pthread_on_pthread_registered, mono_wasm_pthread_set_name, mono_wasm_install_js_worker_interop, mono_wasm_uninstall_js_worker_interop, mono_wasm_start_io_thread_async, mono_wasm_warn_about_blocking_wait
} from "./pthreads";
import { mono_wasm_dump_threads } from "./pthreads/ui-thread";
import { mono_wasm_schedule_synchronization_context } from "./pthreads/shared";
import { mono_wasm_get_locale_info } from "./globalization-locale";

import { mono_wasm_profiler_record, mono_wasm_profiler_now } from "./profiler";
import { ds_rt_websocket_create, ds_rt_websocket_send, ds_rt_websocket_poll, ds_rt_websocket_recv, ds_rt_websocket_close } from "./diagnostics";

// the JS methods would be visible to EMCC linker and become imports of the WASM module

export const mono_wasm_threads_imports = !WasmEnableThreads ? [] : [
    // mono-threads-wasm.c
    mono_wasm_pthread_on_pthread_registered,
    mono_wasm_pthread_on_pthread_attached,
    mono_wasm_pthread_on_pthread_unregistered,
    mono_wasm_pthread_set_name,
    mono_wasm_start_deputy_thread_async,
    mono_wasm_start_io_thread_async,
    mono_wasm_schedule_synchronization_context,

    // mono-threads.c
    mono_wasm_dump_threads,

    // corebindings.c
    mono_wasm_install_js_worker_interop,
    mono_wasm_uninstall_js_worker_interop,
    mono_wasm_invoke_jsimport_MT,
    mono_wasm_warn_about_blocking_wait,
];

export const mono_wasm_imports = [
    // mini-wasm.c
    mono_wasm_schedule_timer,

    // mini-wasm-debugger.c
    mono_wasm_asm_loaded,
    mono_wasm_debugger_log,
    mono_wasm_add_dbg_command_received,
    mono_wasm_fire_debugger_agent_message_with_data,
    mono_wasm_fire_debugger_agent_message_with_data_to_pause,
    // mono-threads-wasm.c
    schedule_background_exec,

    // interp.c and jiterpreter.c
    mono_interp_tier_prepare_jiterpreter,
    mono_interp_record_interp_entry,
    mono_interp_jit_wasm_entry_trampoline,
    mono_interp_jit_wasm_jit_call_trampoline,
    mono_interp_invoke_wasm_jit_call_trampoline,
    mono_interp_flush_jitcall_queue,
    mono_wasm_free_method_data,

    // browser.c, ep-rt-mono-runtime-provider.c
    mono_wasm_profiler_now,
    mono_wasm_profiler_record,

    // driver.c
    mono_wasm_trace_logger,
    mono_wasm_set_entrypoint_breakpoint,

    // src/native/minipal/random.c
    mono_wasm_browser_entropy,

    // mono-proclib.c
    mono_wasm_process_current_pid,

    // corebindings.c
    mono_wasm_console_clear,
    mono_wasm_release_cs_owned_object,
    mono_wasm_bind_js_import_ST,
    mono_wasm_invoke_js_function,
    mono_wasm_invoke_jsimport_ST,
    mono_wasm_resolve_or_reject_promise,
    mono_wasm_cancel_promise,
    mono_wasm_get_locale_info,

    //event pipe
    ds_rt_websocket_create,
    ds_rt_websocket_send,
    ds_rt_websocket_poll,
    ds_rt_websocket_recv,
    ds_rt_websocket_close,
];

// !!! Keep in sync with exports-linker.ts
const wasmImports: Function[] = [
    ...mono_wasm_imports,
    // threading exports, if threading is enabled
    ...mono_wasm_threads_imports,
];

export function replace_linker_placeholders (imports: WebAssembly.Imports) {
    // the output from emcc contains wrappers for these linker imports which add overhead,
    //  but now we have what we need to replace them with the actual functions
    // By default the imports all live inside of 'env', but emscripten minification could rename it to 'a'.
    // See https://github.com/emscripten-core/emscripten/blob/c5d1a856592b788619be11bbdc1dd119dec4e24c/src/preamble.js#L933-L936
    const env = imports.env || imports.a;
    if (!env) {
        mono_log_warn("WARNING: Neither imports.env or imports.a were present when instantiating the wasm module. This likely indicates an emscripten configuration issue.");
        return;
    }

    // the import names could be minified by applyImportAndExportNameChanges in emcc
    // we call each stub function to get the runtime_idx, which is the index into the wasmImports array
    const indexToNameMap: string[] = new Array(wasmImports.length);
    for (const shortName in env) {
        const stub_fn = env[shortName] as Function;
        if (typeof stub_fn === "function" && stub_fn.toString().indexOf("runtime_idx") !== -1) {
            try {
                const { runtime_idx } = stub_fn();
                if (indexToNameMap[runtime_idx] !== undefined) throw new Error(`Duplicate runtime_idx ${runtime_idx}`);
                indexToNameMap[runtime_idx] = shortName;
            } catch {
                // no-action
            }
        }
    }

    for (const [idx, realFn] of wasmImports.entries()) {
        const shortName = indexToNameMap[idx];
        // if it's not found it means the emcc linker didn't include it, which is fine
        if (shortName !== undefined) {
            const stubFn = env[shortName];
            if (typeof stubFn !== "function") throw new Error(`Expected ${shortName} to be a function`);
            env[shortName] = realFn;
        }
    }
}
