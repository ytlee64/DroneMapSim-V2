"""Shared logging helpers for Unreal Python automation scripts."""

import unreal


def make_loggers(scope, warn_label="알림", error_label="오류", error_prefix="", raise_on_error=False):
    """Return (log_info, log_warn, log_error) functions for the given scope."""

    def log_info(msg):
        unreal.log(f">>> [{scope}] {msg}")

    def log_warn(msg):
        unreal.log_warning(f"[{scope} {warn_label}] {msg}")

    def log_error(msg):
        if error_prefix:
            unreal.log_error(f"{error_prefix} [{scope} {error_label}] {msg}")
        else:
            unreal.log_error(f"[{scope} {error_label}] {msg}")

        if raise_on_error:
            raise RuntimeError(msg)

    return log_info, log_warn, log_error