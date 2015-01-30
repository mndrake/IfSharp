c = get_config()
c.KernelManager.kernel_cmd = ["mono", "%s", "{connection_file}"]
c.Session.key = ''
c.Session.keyfile = ''