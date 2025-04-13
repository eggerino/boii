cmake_minimum_required(VERSION 3.11)

project(boii)

add_executable(boii "src/main.c")
target_include_directories(boii PRIVATE "include")
