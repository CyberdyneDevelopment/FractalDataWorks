"""Nothing in the versioning tools names private infrastructure.

public/ is published to GitHub verbatim, and the GitHub sync's leak guard (ci-components github-sync.yml) fails the
pipeline on any line that names the internal Git host product, its domain, the internal network's addresses or the CI clone
token, case-insensitively. A variable or a docstring here is enough to stop every tier and the GitHub publish. These
tools and their tests must never name them: say `internal_root` for the internal side and `mirror_root` for the mirror, as the layout tests do.

The patterns are put together from pieces so that this file does not match them itself.
"""
import os
import re
import unittest

HERE = os.path.dirname(os.path.abspath(__file__))
PRIVATE = re.compile("|".join(["git" + "lab", r"git\.cyberdynedevelopment\.dev", r"10\.10\.10\.", "ci-clone-" + "token"]),
                     re.IGNORECASE)
CI_FILE = re.compile(r"\." + "git" + r"lab-ci\.ya?ml", re.IGNORECASE)  # the guard exempts only the CI file's name


class NoPrivateNamesTests(unittest.TestCase):
    def test_no_file_here_names_private_infrastructure(self):
        hits = []
        for dp, dns, fns in os.walk(HERE):
            dns[:] = [d for d in dns if d not in ("__pycache__", "bin", "obj")]
            for fn in fns:
                path = os.path.join(dp, fn)
                with open(path, encoding="utf-8", errors="replace") as f:
                    for n, line in enumerate(f, 1):
                        if PRIVATE.search(CI_FILE.sub("", line)):
                            hits.append(f"{os.path.relpath(path, HERE)}:{n}: {line.strip()}")
        self.assertEqual(hits, [], "the GitHub sync's leak guard would refuse these lines")


if __name__ == "__main__":
    unittest.main()
