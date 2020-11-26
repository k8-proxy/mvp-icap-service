#ifndef gw_env_var_h
#define gw_env_var_h

int set_from_environment_variable_bool(const char *variable_name, int *target, const int default_value);

#endif